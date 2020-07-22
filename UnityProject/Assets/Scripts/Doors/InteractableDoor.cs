using System.Collections;
using UnityEngine;
using Mirror;
using System;

/// <summary>
///     Allows a door to be interacted with.
///     It also checks for access restrictions on the players ID card
/// </summary>
public class InteractableDoor : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>
{
	private static readonly float weldTime = 5.0f;

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

	HandApply interaction;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		if (interaction.TargetObject != gameObject) return false;
		if (Validations.HasUsedActiveWelder(interaction)) return true;
		if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Crowbar)) return true;
		if (interaction.HandObject != null && interaction.Intent == Intent.Harm) return false; // False to allow melee

		return allowInput && Controller != null;
	}

	public void ClientPredictInteraction(HandApply interaction) {}

	//nothing to rollback
	public void ServerRollbackClient(HandApply interaction) {}

	/// <summary>
	/// Invoke this on server when player bumps into door to try to open it.
	/// </summary>
	public void Bump(GameObject byPlayer)
	{
		if (Controller.IsClosed && Controller.IsAutomatic)
		{
			TryOpen(byPlayer);
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		this.interaction = interaction;

		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
		{
			TryCrowbar();
		}
		else if (!Controller.IsClosed)
		{
			TryClose(); // Close the door if it's open
		}
		else if (Validations.HasUsedActiveWelder(interaction))
		{
			TryWelder(); // Repair or un/weld door, or deconstruct false wall
		}
		// Attempt to open if it's closed
		//Tell the OnAttemptOpen node to activate.
		TryOpen(interaction.Performer);
	}

	/// <summary>
	/// Called on the door to put inputs on it on cooldown.
	/// </summary>
	public void StartInputCoolDown()
	{
		allowInput = false;
		StartCoroutine(DoorInputCoolDown());
	}

	public virtual void TryClose()
	{
		Controller.CloseSignal();
	}

	public virtual void TryOpen(GameObject performer)
	{
		Controller.MobTryOpen(performer);
	}

	public void TryCrowbar()
	{
		if (Controller == null) return;

		if (Controller.IsClosed && Controller.IsHackable && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor ))
        {
			//allows the jaws of life to pry open doors
            ToolUtils.ServerUseToolWithActionMessages(interaction, 4.5f,
            "You start prying open the door...",
            $"{interaction.Performer.ExpensiveName()} starts prying open the door...",
            $"You force the door open with your {gameObject.ExpensiveName()}!",
            $"{interaction.Performer.ExpensiveName()} forces the door open!",
            () =>
            {
                Controller.Open();
            });
		}
		else
		{
			//TODO: force the opening/close if powerless but make sure firelocks are unaffected

			if (!Controller.IsClosed)
			{
				Controller.TryClose();
			}
			else
			{
				Controller.MobTryOpen(interaction.Performer);
			}
		}
	}

	private void TryWelder()
	{
		if (Controller == null) return;

		// We check if intent is harm so that later on, when implemented, we can repair the door.
		if (Controller.IsWeldable && interaction.Intent == Intent.Harm)
		{
			ToolUtils.ServerUseToolWithActionMessages(
					interaction, weldTime,
					$"You start {(Controller.IsWelded ? "unwelding" : "welding")} the door...",
					$"{interaction.Performer.ExpensiveName()} starts {(Controller.IsWelded ? "unwelding" : "welding")} the door...",
					$"You {(Controller.IsWelded ? "unweld" : "weld")} the door.",
					$"{interaction.Performer.ExpensiveName()} {(Controller.IsWelded ? "unwelds" : "welds")} the door.",
					Controller.ServerTryWeld);
		}

		// Start deconstructing the false wall.
		else if (!Controller.IsAutomatic)
		{
			ToolUtils.ServerUseToolWithActionMessages(
					interaction, 4f,
					"You start to disassemble the false wall...",
					$"{interaction.Performer.ExpensiveName()} starts to disassemble the false wall...",
					"You disassemble the girder.",
					$"{interaction.Performer.ExpensiveName()} disassembles the false wall.",
					() => Controller.ServerDisassemble(interaction));
			return;
		}
	}

	/// Disables any interactions with door for a while
	private IEnumerator DoorInputCoolDown()
	{
		yield return WaitFor.Seconds(0.3f);
		allowInput = true;
	}
}
