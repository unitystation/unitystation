using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Sprite_Handler;
using US13.Objects.Doors;
using US13.Systems.Clearance;

namespace US13.Items.Devices
{
	/// <summary>
	/// An item that controls department doors remotely.
	/// </summary>
	public class AccessRemote : NetworkBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>, IClearanceSource
	{
		private AccessRemoteState currentState;

		private SpriteHandler spriteHandler;

		[SerializeField]
		private List<Clearance> clearances = new();

		[SerializeField]
		private SpriteDataSO departmentSprite;

		private void Start()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			if (spriteHandler == null)
			{
				Loggy.Error("[AccessRemote] - Cannot find sprite handler! did you accidentally remove it from this item's children?");
				return;
			}

			if (departmentSprite == null)
			{
				Loggy.Warning("[AccessRemote] - No department sprite found, using default sprite instead. (default sprite could be blank however!)");
				return;
			}
			spriteHandler.SetSpriteSO(departmentSprite);
		}

		private enum AccessRemoteState
		{
			Open,
			Bolts,
			Emergency
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			switch (currentState)
			{
				case AccessRemoteState.Open:
					currentState = AccessRemoteState.Bolts;
					break;
				case AccessRemoteState.Bolts:
					currentState = AccessRemoteState.Emergency;
					break;
				case AccessRemoteState.Emergency:
					currentState = AccessRemoteState.Open;
					break;
				default:
					currentState = AccessRemoteState.Open;
					break;
			}
			Chat.AddExamineMsg(interaction.Performer, $"Remote mode is set to: {currentState.ToString()}.");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (side == NetworkSide.Client)
			{
				if (interaction.IsHighlight || interaction.IsAltClick) return false;
			}

			return Validations.HasComponent<DoorMasterController>(interaction.TargetObject) && Validations.CanApply(interaction.PerformerPlayerScript, interaction.TargetObject, side, false, ReachRange.Unlimited);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			DoorMasterController doorController = interaction.TargetObject.GetComponent<DoorMasterController>();
			if (doorController == null) return;

			Chat.AddExamineMsg(interaction.Performer, $"You use the access remote on the {doorController.DoorName}");

			// Checks if the door allows you to use a remote on it, it needs an access module for instance
			if (doorController.CheckRemoteConnectivity() == false)
			{
				Chat.AddExamineMsg(interaction.Performer, $"The {doorController.DoorName} does not respond.");
				return;
			}

			// Checks the door's access module to see if this remote has the appropriate clearance
			if (doorController.CheckAccess(gameObject) == false)
			{
				Chat.AddExamineMsg(interaction.Performer, "This remote does not contain the required access.");
				return;
			}

			switch (currentState)
			{
				case AccessRemoteState.Open:
					TryOpenDoor(doorController, interaction.Performer);
					break;
				case AccessRemoteState.Emergency:
					doorController.Access.ToggleAuthorizationBypassState();
					break;
				case AccessRemoteState.Bolts:
					if (doorController.Bolts == null)
					{
						Chat.AddExamineMsg(interaction.Performer, $"{doorController.DoorName} doesn't have a bolts module");
						return;
					}
					doorController.Bolts.PulseToggleBolts();
					break;
				default:
					TryOpenDoor(doorController, interaction.Performer);
					break;
			}
		}

		private void TryOpenDoor(DoorMasterController controller, GameObject performer)
		{
			if (controller.IsClosed)
			{
				controller.PulseTryOpen(performer);
				return;
			}
			controller.PulseTryClose(performer);
		}

		public IEnumerable<Clearance> IssuedClearance => clearances;
		public IEnumerable<Clearance> LowPopIssuedClearance => clearances;
	}
}