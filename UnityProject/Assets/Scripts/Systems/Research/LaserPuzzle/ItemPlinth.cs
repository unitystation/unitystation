using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ItemPlinth : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{

	private UniversalObjectPhysics UniversalObjectPhysics;




	public Pickupable DisplayedItem;

	[SyncVar]
	public bool HasItem = false;

	public void Awake()
	{
		UniversalObjectPhysics = this.GetComponentCustom<UniversalObjectPhysics>();
	}

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side))
		{
			return false;
		}


		if (Validations.IsTarget(gameObject, interaction) == false) return false;



		if (interaction.IsAltClick) return false;

		if (HasItem)
		{
			if (interaction.HandSlot.Item != null) return false;
		}
		else
		{
			if (interaction.HandSlot.Item == null) return false;
		}


		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (HasItem)
		{
			Inventory.ServerAdd(DisplayedItem, interaction.HandSlot);
			//Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
		}
		else
		{
			DisplayedItem = interaction.HandSlot.Item;
			Inventory.ServerDrop(interaction.HandSlot);

			UniversalObjectPhysics.BuckleObjectToThis(DisplayedItem.UniversalObjectPhysics);
		}

	}
}
