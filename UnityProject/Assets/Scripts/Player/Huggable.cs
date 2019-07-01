using UnityEngine;

/// <summary>
/// Allows an object to be hugged by a player.
/// </summary>
public class Huggable : MonoBehaviour, IInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var targetPlayerHealth = interaction.TargetObject.GetComponent<PlayerHealth>();

		// Is the target in range for a hug? Is the target conscious for the hug? Is the performer's intent set to help? Is the performer's hand empty?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    targetPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
		    UIManager.CurrentIntent != Intent.Help ||
		    interaction.HandObject != null)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestHug(PlayerManager.LocalPlayerScript.playerName,
			gameObject);
		return true;
	}
}