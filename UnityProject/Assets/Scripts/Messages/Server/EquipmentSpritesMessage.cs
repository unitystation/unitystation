using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EquipmentSpritesMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.EquipmentSpritesMessage;
	public int Reference;
	public int Index;
	public NetworkInstanceId EquipmentObject;
	public NetworkInstanceId Item;

	public override IEnumerator Process()
	{

		yield return WaitFor(EquipmentObject, Item);

		//Logger.Log("bob2?");
		if (Item == NetworkInstanceId.Invalid)
		{
			//Logger.Log("bob3?");
			//Clear slot message			//yield return WaitFor(EquipmentObject);
			if (NetworkObjects[0] != null)
			{
				//Logger.Log("OR?");
				NetworkObjects[0].GetComponent<Equipment>().clothingSlots[Index].SetReference( null);
			}
		}
		else { 
			//yield return WaitFor(EquipmentObject, Item);
			if (NetworkObjects[0] != null)
			{
				//Logger.Log("this? " + NetworkObjects[0].name + " " + NetworkObjects[1].name);
				NetworkObjects[0].GetComponent<Equipment>().clothingSlots[Index].SetReference( NetworkObjects[1]);
			}

		}
	}

	public static EquipmentSpritesMessage SendToAll(GameObject equipmentObject, int index, GameObject _Item)
	{
		var msg = CreateMsg(equipmentObject, index, _Item);
		msg.SendToAll();
		return msg;
	}

	public static EquipmentSpritesMessage SendTo(GameObject equipmentObject, int index, GameObject recipient, GameObject _Item)
	{
		var msg = CreateMsg(equipmentObject, index, _Item);
		msg.SendTo(recipient);
		return msg;
	}

	public static EquipmentSpritesMessage CreateMsg(GameObject equipmentObject, int index, GameObject _Item)
	{
		if (_Item != null)
		{
			return new EquipmentSpritesMessage
			{
				Index = index,
				EquipmentObject = equipmentObject.NetId(),
				Item = _Item.NetId()
			};
		}
		else {
			return new EquipmentSpritesMessage
			{
				Index = index,
				EquipmentObject = equipmentObject.NetId(),
				Item = NetworkInstanceId.Invalid
			};
		}
	}
}
