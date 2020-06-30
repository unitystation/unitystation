using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Mirror;
using UnityEngine;

/// <summary>
/// Main component for chemistry dispenser.
/// </summary>
public class ChemistryDispenser : NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowered
{
	public ReagentContainer Container => itemSlot != null && itemSlot.ItemObject != null
		? itemSlot.ItemObject.GetComponent<ReagentContainer>()
		: null;

	public delegate void ChangeEvent();

	public static event ChangeEvent changeEvent;

	private ItemStorage itemStorage;
	private ItemSlot itemSlot;

	private void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		itemSlot = itemStorage.GetIndexedItemSlot(0);
	}

	private void UpdateGUI()
	{
		// Change event runs updateAll in ChemistryGUI
		if (changeEvent != null)
		{
			changeEvent();
		}
	}

	public void EjectContainer()
	{
		Inventory.ServerDrop(itemSlot);
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		//only interaction that works is using a reagent container on this
		if (!Validations.HasComponent<ReagentContainer>(interaction.HandObject)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//put the reagant container inside me
		Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		UpdateGUI();
	}

	//############################## power stuff ########################################
	public PowerStates ThisState;

	public void PowerNetworkUpdate(float Voltage) { }

	public void StateUpdate(PowerStates State)
	{
		ThisState = State;
	}
}