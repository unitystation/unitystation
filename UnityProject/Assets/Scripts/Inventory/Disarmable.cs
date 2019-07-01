using UnityEngine;

/// <summary>
/// Allows an object to be disarmed by a player.
/// </summary>
public class Disarmable : MonoBehaviour, IInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		// Is the target in range for a disarm? Is the performer's intent set to disarm?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    UIManager.CurrentIntent != Intent.Disarm)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestDisarm(
			interaction.Performer, interaction.TargetObject);
		return true;
	}
}