using System.Collections;
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

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_CONSCIOUS
			.WithValidation(DoesUsedObjectHaveComponent<ReagentContainer>.DOES);
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//put the reagant container inside me
		Container = interaction.UsedObject.GetComponent<ReagentContainer>();
		objectse = interaction.UsedObject.GetComponentInChildren<ObjectBehaviour> ();
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.SlotName);
		InventoryManager.UpdateInvSlot(true, "", interaction.UsedObject, slot.UUID);
		UpdateGUI();
	}
}
