using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class UpdateSlotMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateSlotMessage;
	public NetworkInstanceId Item;
	public NetworkInstanceId Recipient;
	public EquipSlot equipSlot;
	public bool RemoveItem;

	public override IEnumerator Process()
	{
		if (RemoveItem)
		{
			yield return WaitFor(Recipient, Item);
			InventoryManager.ClientClearInvSlot(NetworkObjects[0].GetComponent<PlayerNetworkActions>(), equipSlot);
		}
		else
		{
			yield return WaitFor(Recipient, Item);
			InventoryManager.ClientEquipInInvSlot(NetworkObjects[0].GetComponent<PlayerNetworkActions>(), NetworkObjects[1], equipSlot);
		}
	}

	public static UpdateSlotMessage Send(GameObject recipient, GameObject item, bool removeItem, EquipSlot sentEquipSlot = 0)
	{
		UpdateSlotMessage msg = new UpdateSlotMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			Item = item.GetComponent<NetworkIdentity>().netId,
			RemoveItem = removeItem,
			equipSlot = sentEquipSlot
		};
		msg.SendTo(recipient);
		return msg;
	}
}