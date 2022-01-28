using Doors;
using Doors.Modules;
using UnityEngine;

public class AccessRemote : MonoBehaviour, IClientInteractable<HandActivate>, IClientInteractable<HandApply>
{

	public bool Interact(HandActivate interaction)
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
		}
		Chat.AddExamineMsgFromServer(interaction.Performer, $"Remote mode is set to: {currentState.ToString()}.");

		return true;
	}

	public bool Interact(HandApply interaction)
	{
		if (interaction.IsHighlight || interaction.IsAltClick) //Only work on left clicks
			return false;

		if (Validations.HasComponent<DoorMasterController>(interaction.TargetObject) == false) return false;
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
					Chat.AddExamineMsgFromServer(interaction.Performer, $"{doorController.gameObject.ExpensiveName()} has no access module!");
					return false;
				}
				accessModule.ToggleAuthorizationBypassState();
				break;

			case AccessRemoteState.Bolts:
				BoltsModule boltsModule = interaction.TargetObject.GetComponentInChildren<BoltsModule>();
				if (boltsModule == null)
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, $"{doorController.gameObject.ExpensiveName()} has no bolts module!");
					return false;
				}
				boltsModule.ToggleBolts();
				break;
		}

		Chat.AddExamineMsgFromServer(interaction.Performer, $"You use access remote on: {doorController.gameObject.ExpensiveName()}.");
		return true;
	}

	private enum AccessRemoteState
	{
		Open,
		Bolts,
		Emergency
	}

	private AccessRemoteState currentState = AccessRemoteState.Open;
}
