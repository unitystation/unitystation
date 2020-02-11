﻿using System.Collections;
using UnityEngine;


/// <summary>
///     Allows a door to be interacted with.
///     It also checks for access restrictions on the players ID card
/// </summary>
public class InteractableDoor : MonoBehaviour, IPredictedCheckedInteractable<HandApply>
{
	public bool allowInput = true;

	private DoorController controller;
	public DoorController Controller
	{
		get
		{
			if (!controller)
			{
				controller = GetComponent<DoorController>();
			}
			return controller;
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.TargetObject != gameObject) return false;

		return allowInput && Controller != null;
	}

	public void ClientPredictInteraction(HandApply interaction)
	{
		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
	}

	//nothing to rollback
	public void ServerRollbackClient(HandApply interaction) {}

	/// <summary>
	/// Invoke this on server when player bumps into door to try to open it.
	/// </summary>
	public void Bump(GameObject byPlayer)
	{
		if (!Controller.IsOpened)
		{
			Controller.ServerTryOpen(byPlayer);
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Server actions
		// Close the door if it's open
		if (Controller.IsOpened)
		{
			Controller.ServerTryClose();
		}
		else
		{
			// Attempt to open if it's closed
			Controller.ServerTryOpen(interaction.Performer);
		}

		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
	}

	/// Disables any interactions with door for a while
	private IEnumerator DoorInputCoolDown()
	{
		yield return WaitFor.Seconds(0.3f);
		allowInput = true;
	}
}