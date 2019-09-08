using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IdConsole : NBHandApplyInteractable
{
	public IdConsoleUpdateEvent OnConsoleUpdate = new IdConsoleUpdateEvent();
	public IDCard AccessCard;
	public IDCard TargetCard;
	public bool LoggedIn;

	private void UpdateGUI()
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
		var handIDCard = interaction.HandObject.GetComponent<IDCard>();
		if(handIDCard)
		{
			InsertCard(handIDCard);
		}
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		InventoryManager.ClearInvSlot(slot);
		UpdateGUI();
	}

	/// <summary>
	/// Insert some ID into console and update login details.
	/// Will spit out currently inserted ID card.
	/// </summary>
	///<param name="cardToInsert">Card you want to insert</param>
	private void InsertCard(IDCard cardToInsert)
	{
		if (LoggedIn)
		{
			if (TargetCard)
			{
				EjectCard(TargetCard);
			}
			TargetCard = cardToInsert;
		}
		else
		{
			if (AccessCard)
			{
				EjectCard(AccessCard);
			}
			AccessCard = cardToInsert;
		}
	}

	/// <summary>
	/// Spits out ID card from console and updates login details.
	/// </summary>
	/// <param name="cardToEject">Card you want to eject</param>
	public void EjectCard(IDCard cardToEject)
	{
		ObjectBehaviour objBeh = cardToEject.GetComponentInChildren<ObjectBehaviour>();
		Vector3Int pos = gameObject.RegisterTile().WorldPosition;
		CustomNetTransform netTransform = objBeh.GetComponent<CustomNetTransform>();
		netTransform.AppearAtPosition(pos);
		netTransform.AppearAtPositionServer(pos);
		UpdateGUI();
	}
}

public class IdConsoleUpdateEvent : UnityEvent { }