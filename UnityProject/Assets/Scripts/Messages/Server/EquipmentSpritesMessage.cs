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
		yield return null;
	}

	public static EquipmentSpritesMessage SendToAll(GameObject equipmentObject, int index, int reference)
	{
		EquipmentSpritesMessage msg = new EquipmentSpritesMessage
		{
			Index = index,
			Reference = reference,
			EquipmentObject = equipmentObject.NetId()
		};
		msg.SendToAll();
		return msg;
	}
}
