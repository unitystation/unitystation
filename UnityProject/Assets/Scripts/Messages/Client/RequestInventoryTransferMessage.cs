using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Client tells server to transfer items between 2 item slots.
/// The item slots must be either in the player's slot tree (i.e. currently owned by them
/// even if nested within an item storage) or in an InteractableStorage that this player
/// is an observer of.
/// </summary>
public class RequestInventoryTransferMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.RequestSlotTransferMessage;

	public uint FromStorage;
	public int FromSlotIndex;
	public NamedSlot FromNamedSlot;
	public uint ToStorage;
	public int ToSlotIndex;
	public NamedSlot ToNamedSlot;

	public override IEnumerator Process()
	{
		yield return WaitFor(FromStorage, ToStorage);

		var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), FromNamedSlot, FromSlotIndex);
		var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), ToNamedSlot, ToSlotIndex);

		bool valid = true;
		if (!Validations.CanPutItemToSlot(SentByPlayer.Script, toSlot, fromSlot.Item, NetworkSide.Server))
		{
			LogFail(fromSlot, toSlot);
			yield return null;
		}
		//the slots must both be either in this player's inv or in an observed InteractableStorage
		if (!ValidSlot(toSlot) || !ValidSlot(fromSlot))
		{
			LogFail(fromSlot, toSlot);
			yield return null;
		}

		Inventory.ServerTransfer(fromSlot, toSlot);
	}

	private bool ValidSlot(ItemSlot toCheck)
	{
		var holder = toCheck.GetRootStorage().gameObject;
		//its in their inventory, this is valid
		if (holder == SentByPlayer.GameObject) return true;

		//it's not in their inventory but they may be observing this in an interactable storage
		var interactableStorage = toCheck.ItemStorage != null ? toCheck.ItemStorage.GetComponent<InteractableStorage>() : null;
		if (interactableStorage != null)
		{
			return toCheck.ServerIsObservedBy(SentByPlayer.GameObject);
		}

		return false;
	}

	private void LogFail(ItemSlot fromSlot, ItemSlot toSlot)
	{
		Logger.LogWarningFormat(
			"Possible hacking attempt (or bad clientside logic), {0} tried to transfer from slot {1} to {2} when they" +
			" are not allowed.", Category.Inventory, SentByPlayer.GameObject.name, fromSlot, toSlot);
	}

	/// <summary>
	/// Client tells server to transfer items between 2 item slots.
	/// One of the item slots must be either in this player's slot tree (i.e. currently owned by them
	/// even if nested within an item storage).
	/// </summary>
	/// <param name="fromSlot">
	/// o</param>
	/// <param name="toSlot"></param>
	/// <returns></returns>
	public static void Send(ItemSlot fromSlot, ItemSlot toSlot)
	{
		var player = fromSlot.RootPlayer();
		if (player == null)
		{
			player = toSlot.RootPlayer();
		}
		if (player == null)
		{
			Logger.LogTraceFormat("Client cannot request transfer from {0} to {1} because" +
			                      " neither slot exists in their inventory.", Category.Inventory,
				fromSlot, toSlot);
			return;
		}

		if (!Validations.CanPutItemToSlot(player.GetComponent<PlayerScript>(), toSlot, fromSlot.Item,
			NetworkSide.Client))
		{
			Logger.LogTraceFormat("Client cannot request transfer from {0} to {1} because" +
			                      " validation failed.", Category.Inventory,
				fromSlot, toSlot);
			return;
		}

		RequestInventoryTransferMessage msg = new RequestInventoryTransferMessage
		{
			FromStorage = fromSlot.ItemStorageNetID,
			FromSlotIndex = fromSlot.SlotIdentifier.SlotIndex,
			FromNamedSlot = fromSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			ToStorage = toSlot.ItemStorageNetID,
			ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
			ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};
		msg.Send();
	}
}