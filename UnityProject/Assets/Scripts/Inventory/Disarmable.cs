using UnityEngine;

/// <summary>
/// Allows an object to be disarmed by a player.
/// </summary>
public class Disarmable : MonoBehaviour, IInteractable<PositionalHandApply>
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
		    performerRegisterPlayer.IsDown ||
		    performerRegisterPlayer.IsStunnedClient ||
		    interaction.Performer == interaction.TargetObject)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestDisarm(
			interaction.Performer, interaction.TargetObject);
		return true;
	}
}