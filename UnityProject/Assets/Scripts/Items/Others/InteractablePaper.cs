using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows paper to be displayed via activating it or interacting with it with a pen in hand
/// </summary>
[RequireComponent(typeof(Paper))]
[RequireComponent(typeof(Pickupable))]
public class InteractablePaper : Interactable<HandActivate, InventoryApply>
{
	public NetTabType NetTabType;
	public Paper paper;

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}

	protected override bool WillInteractT2(InventoryApply interaction, NetworkSide side)
	{
		if (!base.WillInteractT2(interaction, side)) return false;
		//only pen can be used on this
		if (!Validations.HasComponent<Pen>(interaction.HandObject)) return false;
		return true;
	}

	protected override void ServerPerformInteraction(InventoryApply interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}
}