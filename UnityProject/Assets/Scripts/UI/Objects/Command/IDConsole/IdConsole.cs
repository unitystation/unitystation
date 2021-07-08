using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IdConsole : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public IdConsoleUpdateEvent OnConsoleUpdate = new IdConsoleUpdateEvent();
	private ItemStorage itemStorage;
	private ItemSlot AccessSlot;
	private ItemSlot TargetSlot;
	public IDCard AccessCard => AccessSlot.Item != null ? AccessSlot.Item.GetComponent<IDCard>() : null;
	public IDCard TargetCard => TargetSlot.Item != null ? TargetSlot.Item.GetComponent<IDCard>() : null;
	public bool LoggedIn;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		AccessSlot = itemStorage.GetIndexedItemSlot(0);
		TargetSlot = itemStorage.GetIndexedItemSlot(1);
		AccessSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
		TargetSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
	}

	private void OnServerSlotContentsChange()
	{
		//propagate the ID change to listeners
		OnConsoleUpdate.Invoke();
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		//interaction only works if using an ID card on console
		if (!Validations.HasComponent<IDCard>(interaction.HandObject))
			return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (LoggedIn)
		{
			if (TargetCard)
			{
				EjectCard(TargetCard, interaction.PerformerPlayerScript.connectedPlayer);
			}

			Inventory.ServerTransfer(interaction.HandSlot, TargetSlot);
		}
		else
		{
			if (AccessCard)
			{
				EjectCard(AccessCard, interaction.PerformerPlayerScript.connectedPlayer);
			}

			Inventory.ServerTransfer(interaction.HandSlot, AccessSlot);
		}
	}
	
	/// <summary>
	/// Return an empty hand slot if available
	/// </summary>
	/// <param name="item"></param>
	/// <param name="subject"></param>
	/// <returns></returns>
	private ItemSlot GetBestSlot(GameObject item, ConnectedPlayer subject)
	{
		if (subject == null)
		{
			return default;
		}

		var playerStorage = subject.Script.DynamicItemStorage;
		return playerStorage.GetBestHandOrSlotFor(item);
	}

	/// <summary>
	/// Spits out ID card from console and updates login details.
	/// </summary>
	/// <param name="cardToEject">Card you want to eject</param>
	public void EjectCard(IDCard cardToEject, ConnectedPlayer subject)
	{
		var slot = cardToEject.GetComponent<Pickupable>().ItemSlot;
		var bestSlot = GetBestSlot(slot.Item.gameObject, subject);
		if (!Inventory.ServerTransfer(slot, bestSlot))
		{
			Inventory.ServerDrop(slot);
		}
		Inventory.ServerDrop(slot);
	}
}

public class IdConsoleUpdateEvent : UnityEvent { }