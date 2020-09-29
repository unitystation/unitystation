using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

[RequireComponent(typeof(ItemStorage))]
public class CommsConsole : MonoBehaviour, ICheckedInteractable<HandApply>
{
	/// <summary>
	/// Fired on server side when ID card is inserted or removed. Provides the new status of id card (null if removed)
	/// </summary>
	[FormerlySerializedAs("IdEvent")] public IDEvent OnServerIDCardChanged = new IDEvent();

	private ItemStorage itemStorage;
	private ItemSlot itemSlot;

	public IDCard IdCard => itemSlot.Item != null ? itemSlot.Item.GetComponent<IDCard>() : null;

	private void Awake()
	{
		//we can just store a single card.
		itemStorage = GetComponent<ItemStorage>();
		itemSlot = itemStorage.GetIndexedItemSlot(0);
		itemSlot.OnSlotContentsChangeServer.AddListener(OnServerSlotContentsChange);
	}

	private void OnServerSlotContentsChange()
	{
		//propagate the ID change to listeners
		OnServerIDCardChanged.Invoke(IdCard);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		//interaction only works if using an ID card on console
		if (!Validations.HasComponent<IDCard>(interaction.HandObject))
			return false;

		if (!Validations.CanFit(itemSlot, interaction.HandObject, side, true))
			return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Eject existing id card if there is one and put new one in
		if (itemSlot.Item != null)
		{
			ServerRemoveIDCard();
		}

		Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
	}

	/// <summary>
	/// Spits out ID card from console and updates login details.
	/// </summary>
	public void ServerRemoveIDCard()
	{
		Inventory.ServerDrop(itemSlot);
	}
}
public class IDEvent : UnityEvent<IDCard> { }