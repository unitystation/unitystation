using UnityEngine;

/// <summary>
/// Allows an object to be patted when on fire.
/// </summary>
public class ExtinguishByHand : MonoBehaviour, ICheckedInteractable<HandApply>
{
	/// <summary>
	/// Fire stacks removed when patting fires (on other people)
	/// </summary>
	private readonly float patFireStacksRemoved = 999f;

	/// <summary>
	/// Fire stacks removed when patting fires (on yourself
	/// </summary>
	private readonly float patFireStacksRemovedSelf = 999f;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		// Can be done with an empty hand and only to LHBs
		if (interaction.HandObject != null) return false;
		if (!Validations.HasComponent<LivingHealthBehaviour>(interaction.TargetObject)) return false;

		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var performerPlayerHealth = interaction.Performer.GetComponent<PlayerHealth>();
		var performerRegisterPlayer = interaction.Performer.GetComponent<RegisterPlayer>();

		if (UIManager.CurrentIntent != Intent.Help ||
			interaction.HandObject != null ||
			performerPlayerHealth.ConsciousState != ConsciousState.CONSCIOUS ||
			performerRegisterPlayer.IsLayingDown)
		{
			return;
		}
		float fireStacksToRemove = interaction.Performer == interaction.TargetObject ? patFireStacksRemovedSelf : patFireStacksRemoved;
		PlayerManager.LocalPlayerScript.playerNetworkActions.CmdRequestPatFire(interaction.TargetObject, fireStacksToRemove);
	}
}