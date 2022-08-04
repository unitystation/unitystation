using System;
using Doors;
using Doors.Modules;
using Mirror;
using UnityEngine;


namespace Items.Devices
{
	/// <summary>
	/// An item that controls department doors remotely.
	/// </summary>
	public class AccessRemote : NetworkBehaviour, IInteractable<HandActivate>, ICheckedInteractable<HandApply>
	{
		[SyncVar(hook = nameof(SyncCurrentRemoteState))]
		private AccessRemoteState currentState;

		private SpriteHandler spriteHandler;

		[SerializeField] private Access access;
		[SerializeField] private SpriteDataSO departmentSprite;

		private void Start()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			if (spriteHandler == null)
			{
				Logger.LogError("[AccessRemote] - Cannot find sprite handler! did you accidentally remove it from this item's children?");
				return;
			}

			if (departmentSprite == null)
			{
				Logger.LogWarning("[AccessRemote] - No department sprite found, using default sprite instead. (default sprite could be blank however!)");
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

		public void ServerPerformInteraction(HandActivate interaction)
		{
			switch (currentState)
			{
				case AccessRemoteState.Open:
					SyncCurrentRemoteState(currentState, AccessRemoteState.Bolts);
					break;
				case AccessRemoteState.Bolts:
					SyncCurrentRemoteState(currentState, AccessRemoteState.Emergency);
					break;
				case AccessRemoteState.Emergency:
					SyncCurrentRemoteState(currentState, AccessRemoteState.Open);
					break;
			}
			Chat.AddExamineMsg(interaction.Performer, $"Remote mode is set to: {currentState.ToString()}.");
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.IsHighlight || interaction.IsAltClick)
				return false;
			if (Validations.HasComponent<DoorMasterController>(interaction.TargetObject) == false)
				return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			DoorMasterController doorController = interaction.TargetObject.GetComponent<DoorMasterController>();
			if(doorController == null) return;
			AccessModule accessModule = interaction.TargetObject.GetComponentInChildren<AccessModule>();
			Chat.AddExamineMsg(interaction.Performer, $"You use access remote on: {doorController.gameObject.ExpensiveName()}");

			switch (currentState)
			{
				case AccessRemoteState.Open:
					TryOpenDoor(doorController, accessModule, interaction.Performer);
					break;
				case AccessRemoteState.Emergency:
					if (accessModule == null)
					{
						Chat.AddExamineMsg(interaction.Performer, $"{doorController.gameObject.ExpensiveName()} has no access module!");
						return;
					}
					accessModule.ToggleAuthorizationBypassState();
					break;
				case AccessRemoteState.Bolts:
					BoltsModule boltsModule = interaction.TargetObject.GetComponentInChildren<BoltsModule>();
					if (boltsModule == null)
					{
						Chat.AddExamineMsg(interaction.Performer, $"{doorController.gameObject.ExpensiveName()} has no bolts module!");
						return;
					}
					boltsModule.ToggleBolts();
					break;
				default:
					TryOpenDoor(doorController, accessModule, interaction.Performer);
					break;
			}
		}

		private void SyncCurrentRemoteState(AccessRemoteState oldState, AccessRemoteState newState)
		{
			//(Max) - Why do we need to keep track of the old state?
			currentState = newState;
		}

		private void TryOpenDoor(DoorMasterController controller, AccessModule module, GameObject performer)
		{
			if (module != null && module.ProcessCheckAccess(access) == false) return;
			if (controller.IsClosed)
			{
				controller.TryOpen(performer);
				return;
			}
			controller.TryClose();
		}
	}
}

/// NOTE FROM MAX ///
/// This should use the signal manager one day ///
/// But it seems like I need to rework signals to use interfaces ///
/// Because changing the base class of componenets doesn't sound fun ///