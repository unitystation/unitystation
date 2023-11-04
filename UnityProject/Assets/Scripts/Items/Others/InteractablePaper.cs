using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using UnityEngine;

/// <summary>
/// Allows paper to be displayed via activating it or interacting with it with a pen in hand
/// </summary>
[RequireComponent(typeof(Paper))]
[RequireComponent(typeof(Pickupable))]
public class InteractablePaper : MonoBehaviour, IInteractable<HandActivate>, ICheckedInteractable<InventoryApply>
{
	public NetTabType NetTabType;
	public Paper paper;

	public void ServerPerformInteraction(HandActivate interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		//only pen can be used on this
		if (!Validations.HasComponent<Pen>(interaction.UsedObject)) return false;
		//only works if pen is in hand
		if (!interaction.IsFromHandSlot) return false;
		return true;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		//show the paper to the client
		TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType, TabAction.Open);
		paper.UpdatePlayer(interaction.Performer);
	}
}