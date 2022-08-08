using System;
using UnityEngine;

/// <summary>
/// Simple hand teleporter for convenience of admins.
/// </summary>
public class AdminHandTeleporter : MonoBehaviour, ICheckedInteractable<AimApply>
{
	public void ServerPerformInteraction(AimApply interaction)
	{
		if (interaction.MouseButtonState == MouseButtonState.PRESS)
		{
			interaction.PerformerPlayerScript.PlayerSync.AppearAtWorldPositionServer(interaction.WorldPositionTarget.RoundToInt().To2(), true);
		}
	}

	public bool WillInteract(AimApply interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}
}
