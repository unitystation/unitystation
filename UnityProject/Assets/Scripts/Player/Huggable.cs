using UnityEngine;

/// <summary>
/// Allows an object to be hugged by a player.
/// </summary>
public class Huggable : MonoBehaviour, IInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var targetPlayerHealth = interaction.TargetObject.GetComponent<PlayerHealth>();
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		// Is the target in range for a hug? Is the target conscious for the hug? Is the performer's intent set to help?
		// Is the performer's hand empty? Is the performer not stunned/downed? Is the performer conscious to perform the interaction?
		// Is the performer interacting with itself?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    targetPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    UIManager.CurrentIntent != Intent.Help ||
		    interaction.HandObject != null ||
		    performerPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    performerRegisterPlayer.IsDown ||
		    performerRegisterPlayer.IsStunnedClient ||
		    interaction.Performer == interaction.TargetObject)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestHug(PlayerManager.LocalPlayerScript.playerName,
			gameObject);
		return true;
	}
}