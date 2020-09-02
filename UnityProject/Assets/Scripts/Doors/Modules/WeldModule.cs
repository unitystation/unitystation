using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;

public class WeldModule : DoorModuleBase
{
	private bool isWelded = false;

	[SerializeField]
	private float weldTime = 5f;

	private DoorAnimatorV2 doorAnimator;

	protected override void Awake()
	{
		base.Awake();
		doorAnimator = GetComponentInParent<DoorAnimatorV2>();
	}
	public override ModuleSignal OpenInteraction(HandApply interaction)
	{
		return ModuleSignal.Continue;
	}

	public override ModuleSignal ClosedInteraction(HandApply interaction)
	{
		if (Validations.HasUsedActiveWelder(interaction))
		{
			TryWeld(interaction);
			return ModuleSignal.Break;
		}
		else if (isWelded)
		{
			UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine, ChatModifier.None, "The door is welded shut.");
		}

		return ModuleSignal.Continue;
	}

	public override bool CanDoorStateChange()
	{
		return !isWelded;
	}

	private void TryWeld(HandApply interaction)
	{
		if (interaction.Intent == Intent.Harm)
		{
			ToolUtils.ServerUseToolWithActionMessages(
				interaction, weldTime,
				$"You start {(isWelded ? "unwelding" : "welding")} the door...",
				$"{interaction.Performer.ExpensiveName()} starts {(isWelded ? "unwelding" : "welding")} the door...",
				$"You {(isWelded ? "unweld" : "weld")} the door.",
				$"{interaction.Performer.ExpensiveName()} {(isWelded ? "unwelds" : "welds")} the door.",
				ToggleWeld);
		}
	}

	private void ToggleWeld()
	{
		if (!master.IsPerformingAction && master.IsClosed)
		{
			isWelded = !isWelded;
			doorAnimator.ToggleWeldOverlay();
		}
	}

}
