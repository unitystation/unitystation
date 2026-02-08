using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Messages.Server;
using US13.Systems.Inventory;
using US13.UI.Core.Net;

namespace US13.Items.Others
{
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
}