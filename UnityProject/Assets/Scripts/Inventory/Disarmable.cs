using UnityEngine;

/// <summary>
/// Allows an object to be disarmed by a player.
/// TODO: Refactor to use IInteractable
/// </summary>
public class Disarmable : MonoBehaviour, IClientInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		// Is the target in range for a disarm? Is the performer's intent set to disarm?
		// Is the performer not stunned/downed? Is the performer conscious to perform the interaction?
		// Is the performer interacting with itself?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    UIManager.CurrentIntent != Intent.Disarm ||
		    performerPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    performerRegisterPlayer.IsLayingDown ||
		    interaction.Performer == interaction.TargetObject)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestDisarm(interaction.TargetObject);
		return true;
	}
}