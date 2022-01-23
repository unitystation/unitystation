using System;
using UnityEngine; 

/// <summary>
/// Simple hand teleporter for convenience of admins.
/// </summary>
public class AdminHandTeleporter : MonoBehaviour, IInteractable<AimApply>
{
	public void ServerPerformInteraction(AimApply interaction)
	{
		if(interaction.MouseButtonState == MouseButtonState.PRESS){
			interaction.PerformerPlayerScript.PlayerSync.SetPosition(interaction.WorldPositionTarget, true); 
		}
	}
}
