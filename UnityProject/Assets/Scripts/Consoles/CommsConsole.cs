using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CommsConsole : NBHandApplyInteractable
{
	public IDEvent IdEvent = new IDEvent();

	private IDCard idCard;
	public IDCard IdCard
	{
		get => idCard;
		set
		{
			idCard = value;
			IdEvent.Invoke( idCard );
		}
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
			InsertID(handIDCard);
		}
		var slot = InventoryManager.GetSlotFromOriginatorHand(interaction.Performer, interaction.HandSlot.equipSlot);
		InventoryManager.ClearInvSlot(slot);
	}

	/// <summary>
	/// Insert some ID into console and update login details.
	/// Will spit out currently inserted ID card.
	/// </summary>
	///<param name="cardToInsert">Card you want to insert</param>
	private void InsertID(IDCard cardToInsert)
	{
		if (IdCard)
		{
			RemoveID();
		}
		IdCard = cardToInsert;
	}

	/// <summary>
	/// Spits out ID card from console and updates login details.
	/// </summary>
	public void RemoveID()
	{
		ObjectBehaviour objBeh = IdCard.GetComponentInChildren<ObjectBehaviour>();
		Vector3Int pos = gameObject.RegisterTile().WorldPosition;
		CustomNetTransform netTransform = objBeh.GetComponent<CustomNetTransform>();
		netTransform.AppearAtPosition(pos);
		netTransform.AppearAtPositionServer(pos);
		IdCard = null;
	}
}
public class IDEvent : UnityEvent<IDCard> { }