using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Server tells client the current status of a particular item slot.
/// </summary>
public class UpdateItemSlotMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateItemSlotMessage;
	public uint Storage;
	public uint Item;
	public int SlotIndex;
	public NamedSlot NamedSlot;

	public override IEnumerator Process()
	{
		yield return WaitFor(Storage, Item);

		ItemSlot slot = null;
		if (SlotIndex == -1)
		{
			slot = ItemSlot.GetNamed(NetworkObjects[0].GetComponent<ItemStorage>(), NamedSlot);
		}
		else
		{
			slot = ItemSlot.GetIndexed(NetworkObjects[0].GetComponent<ItemStorage>(), SlotIndex);
		}

		var pickupable = Item == NetId.Invalid ? null : NetworkObjects[1].GetComponent<Pickupable>();
		slot.ClientUpdate(pickupable);
	}

	/// <summary>
	/// Inform the client about this slot's current status.
	/// </summary>
	/// <param name="recipient">client to inform</param>
	/// <param name="inventorySlot">slot to tell them about</param>
	/// <returns></returns>
	public static void Send(GameObject recipient, ItemSlot itemSlot)
	{
		UpdateItemSlotMessage msg = new UpdateItemSlotMessage
		{
			Storage = itemSlot.ItemStorageNetId,
			Item = itemSlot.Item != null ? itemSlot.Item.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			SlotIndex = itemSlot.SlotIdentifier.SlotIndex,
			NamedSlot = itemSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};
		msg.SendTo(recipient);
	}

	/// <summary>
	/// Inform the clients about this slot's current status.
	/// </summary>
	/// <param name="recipients">clients to inform</param>
	/// <param name="inventorySlot">slot to tell them about</param>
	/// <returns></returns>
	public static void Send(IEnumerable<GameObject> recipients, ItemSlot itemSlot)
	{
		UpdateItemSlotMessage msg = new UpdateItemSlotMessage
		{
			Storage = itemSlot.ItemStorageNetId,
			Item = itemSlot.Item != null ? itemSlot.Item.GetComponent<NetworkIdentity>().netId : NetId.Invalid,
			SlotIndex = itemSlot.SlotIdentifier.SlotIndex,
			NamedSlot = itemSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};

		foreach (var recipient in recipients)
		{
			msg.SendTo(recipient);
		}
	}
}