using System.Collections.Generic;
using Messages.Server;
using UnityEngine;

namespace Doors.Modules
{
	public class WeldModule : DoorModuleBase
	{
		private bool isWelded = false;

		[SerializeField] [Tooltip("Base time this door takes to be welded")]
		private float weldTime = 5f;//TODO use time multipliers from welder tools

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;
			if (Validations.HasUsedActiveWelder(interaction))
			{
				TryWeld(interaction);
				return ModuleSignal.Break;
			}

			if (isWelded)
			{
				UpdateChatMessage.Send(
					interaction.Performer,
					ChatChannel.Examine,
					ChatModifier.None,
					"The door is welded shut.");
			}

			return ModuleSignal.Continue;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		public override bool CanDoorStateChange()
		{
			return !isWelded;
		}

		private void TryWeld(HandApply interaction)
		{
			switch (interaction.Intent)
			{
				case Intent.Harm:
					ToolUtils.ServerUseToolWithActionMessages(
						interaction, weldTime,
						$"You start {(isWelded ? "unwelding" : "welding")} the door...",
						$"{interaction.Performer.ExpensiveName()} starts {(isWelded ? "unwelding" : "welding")} the door...",
						$"You {(isWelded ? "unweld" : "weld")} the door.",
						$"{interaction.Performer.ExpensiveName()} {(isWelded ? "unwelds" : "welds")} the door.",
						ToggleWeld);
					break;
				case Intent.Help:
					//TODO add repairing door logic here
					break;
			}
		}

		private void ToggleWeld()
		{
			if (master.IsPerformingAction || !master.IsClosed)
			{
				return;
			}

			isWelded = !isWelded;

			if (isWelded)
			{
				master.DoorAnimator.AddWeldOverlay();
			}
			else
			{
				master.DoorAnimator.RemoveWeldOverlay();
			}
		}

	}
}
