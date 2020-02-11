using UnityEngine;

/// <summary>
/// Allows an object to be pet by a player. Shameless copy of Huggable.cs
/// </summary>
public class Pettable : MonoBehaviour, IClientInteractable<PositionalHandApply>
{
	public bool Interact(PositionalHandApply interaction)
	{
		var targetNPCHealth = interaction.TargetObject.GetComponent<LivingHealthBehaviour>();
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		// Is the target in range for a pet? Is the target conscious for the pet? Is the performer's intent set to help?
		// Is the performer's hand empty? Is the performer not stunned/downed? Is the performer conscious to perform the interaction?
		// Is the performer interacting with itself?
		if (!PlayerManager.LocalPlayerScript.IsInReach(interaction.WorldPositionTarget, false) ||
			targetNPCHealth.ConsciousState != ConsciousState.CONSCIOUS ||
			UIManager.CurrentIntent != Intent.Help ||
			interaction.HandObject != null ||
			performerPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
			performerRegisterPlayer.IsLayingDown ||
			interaction.Performer == interaction.TargetObject)
		{
			return false;
		}

		Chat.AddActionMsgToChat(interaction.Performer,
	$"You pet {interaction.TargetObject.name}.", $"{interaction.Performer.ExpensiveName()} pets {interaction.TargetObject.name}.");
		return true;
	}
}