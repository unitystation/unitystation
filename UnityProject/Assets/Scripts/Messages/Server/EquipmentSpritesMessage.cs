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


		if (Item == NetworkInstanceId.Invalid)
		{
			//Clear slot message			//yield return WaitFor(EquipmentObject);
			if (NetworkObject != null)
			{
				Logger.Log("OR?");
				NetworkObject.GetComponent<Equipment>().clothingSlots[Index].SetReference(Reference, null);
			}
		}
		else { 
			//yield return WaitFor(EquipmentObject, Item);
			if (NetworkObjects[0] != null)
			{
				Logger.Log("this? " + NetworkObjects[0].name + " " + NetworkObjects[1].name);
				NetworkObjects[0].GetComponent<Equipment>().clothingSlots[Index].SetReference(Reference, NetworkObjects[1]);
			}

		}
	}

	public static EquipmentSpritesMessage SendToAll(GameObject equipmentObject, int index, int reference, GameObject _Item)
	{
		var msg = CreateMsg(equipmentObject, index, reference,_Item);
		msg.SendToAll();
		return msg;
	}

	public static EquipmentSpritesMessage SendTo(GameObject equipmentObject, int index, int reference, GameObject recipient, GameObject _Item)
	{
		var msg = CreateMsg(equipmentObject, index, reference,_Item);
		msg.SendTo(recipient);
		return msg;
	}

	public static EquipmentSpritesMessage CreateMsg(GameObject equipmentObject, int index, int reference, GameObject _Item)
	{
		if (_Item != null)
		{
			return new EquipmentSpritesMessage
			{
				Index = index,
				Reference = reference,
				EquipmentObject = equipmentObject.NetId(),
				Item = _Item.NetId()
			};
		}
		else {
			return new EquipmentSpritesMessage
			{
				Index = index,
				Reference = reference,
				EquipmentObject = equipmentObject.NetId(),
				Item = NetworkInstanceId.Invalid
			};
		}
	}
}
