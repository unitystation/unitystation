using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message telling a player the new outward appearance of a player based on their equipped sprites.
///
///
///TODO: There is a security issue here which existed even prior to inventory refactor.
///The issue is that every time a player's top level inventory changes, this message is sent to all other players
///telling them what object was added / removed from that player's slot. So with a hacked client,
///it would be easy to see every item change that happens on the server in top level inventory, such as being able
///to see who is using antag items.
///Bubbling should help prevent this
/// </summary>
public class EquipmentSpritesMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.EquipmentSpritesMessage;
	public NamedSlot NamedSlot;
	public uint EquipmentObject;
	public uint ItemNetID;
	public bool ForceInit;

	//Is this for the body parts or for the clothing items:
	public bool IsBodySprites;

	public override IEnumerator Process()
	{
		yield return WaitFor(EquipmentObject, ItemNetID);
		//Debug.Log(
		//	$"Received EquipMsg: Index {Index} ItemID: {ItemNetID} EquipID: {EquipmentObject} ForceInit: {ForceInit} IsBody: {IsBodySprites}");

		if (NetworkObjects[0] != null)
		{
			if (!IsBodySprites)
			{
				ClothingItem c = NetworkObjects[0].GetComponent<Equipment>().GetClothingItem(NamedSlot);
				if (ItemNetID == NetId.Invalid)
				{
					if (!ForceInit) c.SetReference(null);
				}
				else
				{
					c.SetReference(NetworkObjects[1]);
				}

				if (ForceInit) c.PushTexture();
			}
			else
			{
				ClothingItem c = NetworkObjects[0].GetComponent<PlayerSprites>().characterSprites[(int)NamedSlot];
				if (ItemNetID == NetId.Invalid)
				{
					if (!ForceInit) c.SetReference(null);
				}
				else
				{
					c.SetReference(NetworkObjects[1]);
				}

				if (ForceInit) c.PushTexture();
			}
		}
	}

	public static EquipmentSpritesMessage SendToAll(GameObject equipmentObject, NamedSlot namedSlot, GameObject _Item,
		bool _forceInit = false, bool _isBodyParts = false)
	{
		var msg = CreateMsg(equipmentObject, namedSlot, _Item, _forceInit, _isBodyParts);
		msg.SendToAll();
		return msg;
	}

	public static EquipmentSpritesMessage SendTo(GameObject equipmentObject, NamedSlot namedSlot, GameObject recipient,
		GameObject _Item, bool _forceInit, bool _isBodyParts)
	{
		var msg = CreateMsg(equipmentObject, namedSlot, _Item, _forceInit, _isBodyParts);
		msg.SendTo(recipient);
		return msg;
	}

	public static EquipmentSpritesMessage CreateMsg(GameObject equipmentObject, NamedSlot namedSlot, GameObject _Item,
		bool _forceInit, bool _isBodyParts)
	{
		if (_Item != null)
		{
			return new EquipmentSpritesMessage
			{
				NamedSlot = namedSlot,
				EquipmentObject = equipmentObject.NetId(),
				ItemNetID = _Item.NetId(),
				ForceInit = _forceInit,
				IsBodySprites = _isBodyParts
			};
		}
		else
		{
			return new EquipmentSpritesMessage
			{
				NamedSlot = namedSlot,
				EquipmentObject = equipmentObject.NetId(),
				ItemNetID = NetId.Invalid,
				ForceInit = _forceInit,
				IsBodySprites = _isBodyParts
			};
		}
	}
}