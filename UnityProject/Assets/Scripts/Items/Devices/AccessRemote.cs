using Doors;
using Doors.Modules;
using Mirror;
using UnityEngine;

public class AccessRemote : NetworkBehaviour, IInteractable<HandActivate>, ICheckedInteractable<HandApply>
{

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

		switch (currentState)
		{
			case AccessRemoteState.Open:
				if (doorController.IsClosed)
					doorController.TryOpen(interaction.Performer);
				else
					doorController.TryClose();
				break;

			case AccessRemoteState.Emergency:
				AccessModule accessModule = interaction.TargetObject.GetComponentInChildren<AccessModule>();
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
		}

		Chat.AddExamineMsg(interaction.Performer, $"You use access remote on: {doorController.gameObject.ExpensiveName()}");
	}

	private enum AccessRemoteState
	{
		Open,
		Bolts,
		Emergency
	}

	[SyncVar(hook = nameof(SyncCurrentRemoteState))]
	private AccessRemoteState currentState;

	private void SyncCurrentRemoteState(AccessRemoteState oldState, AccessRemoteState newState)
	{
		currentState = newState;
	}
}
