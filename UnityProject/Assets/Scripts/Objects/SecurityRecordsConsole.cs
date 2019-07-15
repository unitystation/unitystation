using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SecurityRecordsConsole : NBHandApplyInteractable
{
	public IDCard IdCard;
	public SecurityRecordsUpdateEvent OnConsoleUpdate = new SecurityRecordsUpdateEvent();

	private void  UpdateGUI()
	{
		OnConsoleUpdate?.Invoke();
	}

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side))
			return false;

		//interaction only works if using an ID card on console
		if (!Validations.HasComponent<IDCard>(interaction.HandObject))
			return false;

		return true;
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//Put ID card inside
		IdCard = interaction.HandObject.GetComponent<IDCard>();
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.SlotName);
		InventoryManager.UpdateInvSlot(true, "", interaction.HandObject, slot.UUID);
		UpdateGUI();
	}
}

public class SecurityRecordsUpdateEvent : UnityEvent { }