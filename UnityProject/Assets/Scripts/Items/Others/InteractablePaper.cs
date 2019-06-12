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

	protected override InteractionValidationChain<HandActivate> InteractionValidationChain()
	{
		//no validations for activate
		return InteractionValidationChain<HandActivate>.EMPTY;
	}

	protected override void ServerPerformInteraction(HandActivate interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}

	protected override InteractionValidationChain<InventoryApply> InteractionValidationChainT2()
	{
		return InteractionValidationChain<InventoryApply>.Create()
			//show the paper if a pen is used on this paper
			.WithValidation(DoesUsedObjectHaveComponent<Pen>.DOES);
	}

	protected override void ServerPerformInteraction(InventoryApply interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}
}