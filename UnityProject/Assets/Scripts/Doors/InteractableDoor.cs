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

	protected override InteractionValidationChain<HandApply> InteractionValidationChain()
	{
		return CommonValidationChains.CAN_APPLY_HAND_SOFT_CRIT
			.WithValidation(TargetIs.GameObject(gameObject))
			.WithValidation(IsInputAllowed);
	}

	private ValidationResult IsInputAllowed(HandApply interaction, NetworkSide side)
	{
		return allowInput && Controller != null ? ValidationResult.SUCCESS : ValidationResult.FAIL;
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
			Controller.TryOpen(byPlayer, null);//fixme: hand can be null
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
			Controller.TryOpen(interaction.Performer, interaction.HandSlot.SlotName);//fixme: hand can be null
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
