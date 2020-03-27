using System.Collections;
using UnityEngine;
using Mirror;


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
		if (Controller.IsClosed && Controller.IsAutomatic)
		{
			Controller.ServerTryOpen(byPlayer);
		}
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		//Server actions
		// Close the door if it's open
		if (!Controller.IsClosed)
		{
			Controller.ServerTryClose();
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
							Chat.AddExamineMsgFromServer(interaction.Performer, "You " + (Controller.IsWelded ? "unweld" : "weld") + " the door.");
							Controller.ServerTryWeld();
						}

						var bar = StandardProgressAction.CreateForWelder(ProgressConfig, ProgressComplete, welder)
						.ServerStartProgress(interaction.Performer.transform.position, weldTime, interaction.Performer);
						if (bar != null)
						{
							SoundManager.PlayNetworkedAtPos("Weld", interaction.Performer.transform.position, Random.Range(0.8f, 1.2f));
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

