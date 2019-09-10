using System.Collections;
using UnityEngine;


/// <summary>
///     Allows a door to be interacted with.
///     It also checks for access restrictions on the players ID card
/// </summary>
public class InteractableDoor : Interactable<HandApply>
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

	protected override bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!base.WillInteract(interaction, side)) return false;

		if (interaction.TargetObject != gameObject) return false;

		return allowInput && Controller != null;
	}

	protected override void ClientPredictInteraction(HandApply interaction)
	{
		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
	}

	/// <summary>
	/// Invoke this on server when player bumps into door to try to open it.
	/// </summary>
	public void Bump(GameObject byPlayer)
	{
		if (!Controller.IsOpened)
		{
			Controller.TryOpen(byPlayer);
		}
	}

	protected override void ServerPerformInteraction(HandApply interaction)
	{
		//Server actions
		// Close the door if it's open
		if (Controller.IsOpened)
		{
			Controller.TryClose();
		}
		else
		{
			// Attempt to open if it's closed
			Controller.TryOpen(interaction.Performer);
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