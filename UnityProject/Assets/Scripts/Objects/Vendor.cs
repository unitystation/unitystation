using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Main component for vending machine object (other logic is in GUI_Vendor). Allows restocking
/// when clicking on vendor with a VendingRestock item in hand.
/// </summary>
[RequireComponent(typeof(HasNetworkTab))]
public class Vendor : NBHandApplyInteractable
{
	public List<VendorItem> VendorContent = new List<VendorItem>();
	public Color HullColor = Color.white;
	public bool EjectObjects = false;
	public EjectDirection EjectDirection = EjectDirection.None;
	public VendorUpdateEvent OnRestockUsed = new VendorUpdateEvent();

	private void Awake()
	{
		//ensure we have a net tab set up with the correct type
		var hasNetTab = GetComponent<HasNetworkTab>();
		if (hasNetTab == null)
		{
			hasNetTab = gameObject.AddComponent<HasNetworkTab>();
		}

		hasNetTab.NetTabType = NetTabType.Vendor;
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;
		if (!Validations.HasComponent<VendingRestock>(interaction.HandObject)) return false;
		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//Checking restock
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		var restock = slot.Item?.GetComponentInChildren<VendingRestock>();
		if (restock != null)
		{
			OnRestockUsed?.Invoke();
			InventoryManager.ClearInvSlot(slot);
		}
	}
}

public enum EjectDirection { None, Up, Down, Random }

public class VendorUpdateEvent: UnityEvent {}

//Adding this as a separate class so we can easily extend it in future -
//add price or required access, stock amount and etc.
[System.Serializable]
public class VendorItem
{
	public GameObject Item;
	public int Stock = 5;

	public VendorItem(VendorItem item)
	{
		this.Item = item.Item;
		this.Stock = item.Stock;
	}
}