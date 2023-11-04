using System.Collections.Generic;
using Doors;
using Doors.Modules;
using Logs;
using Mirror;
using Systems.Clearance;
using UnityEngine;


namespace Items.Devices
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
				Loggy.LogError("[AccessRemote] - Cannot find sprite handler! did you accidentally remove it from this item's children?");
				return;
			}

			if (departmentSprite == null)
			{
				Loggy.LogWarning("[AccessRemote] - No department sprite found, using default sprite instead. (default sprite could be blank however!)");
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

		//FIXME: this shouldn't make its own check and use doors like this. Refactor doors to allow interacting with them with devices instead
		// and let the door handle the interaction.
		public void ServerPerformInteraction(HandApply interaction)
		{
			DoorMasterController doorController = interaction.TargetObject.GetComponent<DoorMasterController>();
			if(doorController == null) return;

			var accessModule = interaction.TargetObject.GetComponentInChildren<AccessModule>();
			if(accessModule == null) return;

			var restricted = interaction.TargetObject.GetComponentInChildren<ClearanceRestricted>();

			Chat.AddExamineMsg(interaction.Performer, $"You use access remote on: {doorController.gameObject.ExpensiveName()}");

			if (restricted != null && restricted.HasClearance(this) == false)
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
					if (restricted == null)
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
					TryOpenDoor(doorController, interaction.Performer);
					break;
			}
		}

		private void TryOpenDoor(DoorMasterController controller, GameObject performer)
		{
			if (controller.IsClosed)
			{
				controller.TryOpen(performer);
				return;
			}
			controller.TryClose();
		}

		public IEnumerable<Clearance> IssuedClearance => clearances;
		public IEnumerable<Clearance> LowPopIssuedClearance => clearances;
	}
}