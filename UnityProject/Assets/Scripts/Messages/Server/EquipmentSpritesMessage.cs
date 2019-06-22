using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class EquipmentSpritesMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.EquipmentSpritesMessage;
	public int Reference;
	public int Index;
	public NetworkInstanceId EquipmentObject;

	public override IEnumerator Process()
	{
		yield return WaitFor(EquipmentObject);

		if (NetworkObject != null)
		{
			NetworkObject.GetComponent<Equipment>().clothingSlots[Index].SetReference(Reference);
		}
	}

	public static EquipmentSpritesMessage SendToAll(GameObject equipmentObject, int index, int reference)
	{
		var msg = CreateMsg(equipmentObject, index, reference);
		msg.SendToAll();
		return msg;
	}

	public static EquipmentSpritesMessage SendTo(GameObject equipmentObject, int index, int reference, GameObject recipient)
	{
		var msg = CreateMsg(equipmentObject, index, reference);
		msg.SendTo(recipient);
		return msg;
	}

	public static EquipmentSpritesMessage CreateMsg(GameObject equipmentObject, int index, int reference)
	{
		return new EquipmentSpritesMessage
		{
			Index = index,
			Reference = reference,
			EquipmentObject = equipmentObject.NetId()
		};
	}
}
