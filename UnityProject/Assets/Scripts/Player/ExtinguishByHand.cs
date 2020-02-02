using UnityEngine;

/// <summary>
/// Allows an object to be patted when on fire.
/// </summary>
public class ExtinguishByHand : MonoBehaviour, IClientInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var targetPlayerHealth = interaction.TargetObject.GetComponent<PlayerHealth>();
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		// Is the target in range for a pat? Is the performer's intent set to help?
		// Is the performer's hand empty? Is the performer not stunned/downed? Is the performer conscious to perform the interaction?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    UIManager.CurrentIntent != Intent.Help ||
		    interaction.HandObject != null ||
		    performerPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    performerRegisterPlayer.IsLayingDown)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestPatFire(PlayerManager.LocalPlayerScript.playerName,
			gameObject);
		return true;
	}
}