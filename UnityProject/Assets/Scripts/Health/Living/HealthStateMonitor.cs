using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System;

/// <summary>
///		Health Monitoring component for all Living entities
///     Monitors the state of the entities health on the server and acts accordingly
/// </summary>
public class HealthStateMonitor : ManagedNetworkBehaviour
{

	//Sends msg to the owner of this player to update their UI
	[Server]
	private void UpdateClientUI(int newHealth)
	{
		UpdateUIMessage.SendHealth(gameObject, newHealth);
	}
}


//TODO things to update:
// OverallHealth
// Conscious State, which handles IsDead and IsCrit
// IsBreathing
// IsSuffocating
// bloodSystem.HeartStopped

//Events:
// Crit()
// Death()