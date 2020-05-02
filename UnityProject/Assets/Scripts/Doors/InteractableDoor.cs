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
	private static readonly StandardProgressActionConfig ProgressConfig =
	new StandardProgressActionConfig(StandardProgressActionType.Construction, allowMultiple: true);

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

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		if (interaction.TargetObject != gameObject) return false;

		return allowInput && Controller != null;
	}

	public void ClientPredictInteraction(HandApply interaction)
	{
		StartInputCoolDown();
	}

	//nothing to rollback
	public void ServerRollbackClient(HandApply interaction) {}

	/// <summary>
	/// Invoke this on server when player bumps into door to try to open it.
	/// </summary>
	public void Bump(GameObject byPlayer)
	{
		if (Controller.IsClosed && Controller.IsAutomatic)
		{
			if (Controller.IsHackable)
			{
				HackingNode onAttemptOpen = Controller.HackingProcess.GetNodeWithInternalIdentifier("OnAttemptOpen");
				onAttemptOpen.SendOutputToConnectedNodes(byPlayer);
			}
			else
			{
				Controller.ServerTryOpen(byPlayer);
			}
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
		{
			if (Controller.IsHackable)
			{
				HackingNode onAttemptClose = Controller.HackingProcess.GetNodeWithInternalIdentifier("OnAttemptClose");
				onAttemptClose.SendOutputToConnectedNodes(interaction.Performer);
			}
			else
			{
				TryCrowbar(interaction.Performer);
			}	
		}
		else if (!Controller.IsClosed)
		{
			TryClose(); // Close the door if it's open
		}
		else
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) // welding the door (only if closed and not helping)
			{
				if (Controller.IsWeldable)
				{
					var welder = interaction.HandObject.GetComponent<Welder>();
					if (welder.IsOn && interaction.Intent != Intent.Help)
					{
						void ProgressComplete()
						{
							if (Controller != null)
							{
								Chat.AddExamineMsgFromServer(interaction.Performer,
									"You " + (Controller.IsWelded ? "unweld" : "weld") + " the door.");
								Controller.ServerTryWeld();
							}
						}

						var bar = StandardProgressAction.CreateForWelder(ProgressConfig, ProgressComplete, welder)
						.ServerStartProgress(interaction.Performer.transform.position, weldTime, interaction.Performer);
						if (bar != null)
						{
							SoundManager.PlayNetworkedAtPos("Weld", interaction.Performer.transform.position, UnityEngine.Random.Range(0.8f, 1.2f), sourceObj: interaction.Performer);
							Chat.AddExamineMsgFromServer(interaction.Performer, "You start " + (Controller.IsWelded ? "unwelding" : "welding") + " the door...");
						}

						return;
					}
				}
				else if (!Controller.IsAutomatic)
				{
					ToolUtils.ServerUseToolWithActionMessages(interaction, 4f,
					"You start to disassemble the false wall...",
					$"{interaction.Performer.ExpensiveName()} starts to disassemble the false wall...",
					"You disassemble the girder.",
					$"{interaction.Performer.ExpensiveName()} disassembles the false wall.",
					() => Controller.ServerDisassemble(interaction));
					return;
				}
			}

			// Attempt to open if it's closed
			//Tell the OnAttemptOpen node to activate.
			if (Controller.IsHackable)
			{
				HackingNode onAttemptOpen = Controller.HackingProcess.GetNodeWithInternalIdentifier("OnAttemptOpen");
				onAttemptOpen.SendOutputToConnectedNodes(interaction.Performer);
			}
			else
			{
				Controller.ServerTryOpen(interaction.Performer);
			}
		}

		StartInputCoolDown();
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
		Controller.ServerTryClose();
	}

	public virtual void TryOpen(GameObject performer)
	{
		Controller.ServerTryOpen(performer);
	}

	public void TryCrowbar(GameObject performer)
	{
		//TODO: force the opening/close if powerless but make sure firelocks are unaffected

		if (!Controller.IsClosed)
		{
			Controller.ServerTryClose();
		}
		else
		{
			Controller.ServerTryOpen(performer);
		}
	}

	/// Disables any interactions with door for a while
	private IEnumerator DoorInputCoolDown()
	{
		yield return WaitFor.Seconds(0.3f);
		allowInput = true;
	}
}

