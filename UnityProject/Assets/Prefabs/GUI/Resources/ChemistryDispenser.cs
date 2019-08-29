﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Main component for chemistry dispenser.
/// </summary>
public class ChemistryDispenser : NBHandApplyInteractable {

	public ReagentContainer Container;
	public ObjectBehaviour objectse;
	public delegate void ChangeEvent ();
	public static event ChangeEvent changeEvent;

	private void  UpdateGUI()
	{
		// Change event runs updateAll in ChemistryGUI
   		if(changeEvent!=null)
		{
			changeEvent();
		}
 	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		//only interaction that works is using a reagent container on this
		if (!Validations.HasComponent<ReagentContainer>(interaction.HandObject)) return false;

		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//put the reagant container inside me
		Container = interaction.HandObject.GetComponent<ReagentContainer>();
		objectse = interaction.HandObject.GetComponentInChildren<ObjectBehaviour> ();
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		InventoryManager.ClearInvSlot(slot);
		UpdateGUI();
	}
}
