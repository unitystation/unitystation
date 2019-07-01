using UnityEngine;

/// <summary>
/// Allows an object to be CPRed by a player.
/// </summary>
public class CPRable : MonoBehaviour, IInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var targetPlayerHealth = interaction.TargetObject.GetComponent<PlayerHealth>();

		// Is the target in range for CPR? Is the target unconscious? Is the intent set to help? Is the target a player? Is the performer's hand empty?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
		    targetPlayerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
		    targetPlayerHealth.ConsciousState == ConsciousState.DEAD ||
		    UIManager.CurrentIntent != Intent.Help ||
		    !targetPlayerHealth ||
		    interaction.HandObject != null)
		{
			return false;
		}

		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestCPR(interaction.Performer,
			interaction.TargetObject);
		return true;
	}
}