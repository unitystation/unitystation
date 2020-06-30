using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message telling a player the new outward appearance of a player, which includes
/// their body part sprites (PlayerSprites.characterSprites) and their
/// equipment (Equipment.ClothingItems)
///
///
///TODO: There is a security issue here which existed even prior to inventory refactor.
///The issue is that every time a player's top level inventory changes, this message is sent to all other players
///telling them what object was added / removed from that player's slot. So with a hacked client,
///it would be easy to see every item change that happens on the server in top level inventory, such as being able
///to see who is using antag items.
///Bubbling should help prevent this
/// </summary>
public class PlayerAppearanceMessage : ServerMessage
{
	//if IsBodySprites, index in PlayerSprites.characterSprites to update.
	//otherwise, ordinal value of NamedSlot enum in Equipment to update
	public int Index;
	public uint EquipmentObject;
	public uint ItemNetID;
	public bool ForceInit;

	//Is this for the body parts or for the clothing items:
	public bool IsBodySprites;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] {EquipmentObject, ItemNetID});
		//Debug.Log(
		//	$"Received EquipMsg: Index {Index} ItemID: {ItemNetID} EquipID: {EquipmentObject} ForceInit: {ForceInit} IsBody: {IsBodySprites}");

		if (NetworkObjects[0] != null)
		{
			if (!IsBodySprites)
			{
				ClothingItem c = NetworkObjects[0].GetComponent<Equipment>().GetClothingItem((NamedSlot) Index);
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
				ClothingItem c = NetworkObjects[0].GetComponent<PlayerSprites>().characterSprites[Index];
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

	public static PlayerAppearanceMessage SendToAll(GameObject equipmentObject, int index, GameObject _Item,
		bool _forceInit = false, bool _isBodyParts = false)
	{
		var msg = CreateMsg(equipmentObject, index, _Item, _forceInit, _isBodyParts);
		msg.SendToAll();
		return msg;
	}

	public static PlayerAppearanceMessage SendTo(GameObject equipmentObject, int index, NetworkConnection recipient,
		GameObject _Item, bool _forceInit, bool _isBodyParts)
	{
		var msg = CreateMsg(equipmentObject, index, _Item, _forceInit, _isBodyParts);
		msg.SendTo(recipient);
		return msg;
	}

	public static PlayerAppearanceMessage CreateMsg(GameObject equipmentObject, int index, GameObject _Item,
		bool _forceInit, bool _isBodyParts)
	{
		if (_Item != null)
		{
			return new PlayerAppearanceMessage
			{
				Index = index,
				EquipmentObject = equipmentObject.NetId(),
				ItemNetID = _Item.NetId(),
				ForceInit = _forceInit,
				IsBodySprites = _isBodyParts
			};
		}
		else
		{
			return new PlayerAppearanceMessage
			{
				Index = index,
				EquipmentObject = equipmentObject.NetId(),
				ItemNetID = NetId.Invalid,
				ForceInit = _forceInit,
				IsBodySprites = _isBodyParts
			};
		}
	}
}