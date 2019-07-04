using System;
using UnityEngine;

/// <summary>
/// Indicates that the object is a source of welding fuel
/// </summary>
public class WeldingFuelSource : MonoBehaviour, IInteractable<HandApply>
{

	public bool Interact(HandApply interaction)
	{
		if (!DefaultWillInteract.HandApply(interaction, NetworkSide.Client)) return false;

		if (interaction.TargetObject != gameObject) return false;

		if (!Validations.HasComponent<Welder>(interaction.HandObject)) return false;

		PlayerManager.PlayerScript.playerNetworkActions.CmdRefillWelder(interaction.HandObject, gameObject);
		return true;
	}
}