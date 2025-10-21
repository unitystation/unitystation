using System.Collections.Generic;
using Messages.Server;
using UnityEngine;

namespace Doors.Modules
{
	public class WeldModule : DoorModuleBase
	{
		private bool isWelded = false;
		public bool IsWelded => isWelded;

		[SerializeField] [Tooltip("Base time this door takes to be welded")]
		private float weldTime = 5f;//TODO use time multipliers from welder tools
		[SerializeField] [Tooltip("Maximum time this door takes to be repaired")]
		private float repairTime = 10f;
		private string doorName;
		private Integrity integrity;

		protected override void Awake()
		{
			base.Awake();

			doorName = transform.parent.gameObject.ExpensiveName();

			integrity = GetComponentInParent<Integrity>();
		}

		public override void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (isWelded)
			{
				States.Add(DoorProcessingStates.Welded);
			}

		}

		public override void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return;
			if (Validations.HasUsedActiveWelder(interaction))
			{
				TryWeld(interaction, States);
			}

			if (isWelded)
			{
				States.Add(DoorProcessingStates.Welded);
			}
		}

		public override void BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			if (isWelded)
			{
				States.Add(DoorProcessingStates.Welded);
			}
		}



		private void TryWeld(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			switch (interaction.Intent)
			{
				// Harm intent welds/unwelds the door
				case Intent.Harm:
					ToolUtils.ServerUseToolWithActionMessages(
						interaction, weldTime,
						$"You start {(isWelded ? "unwelding" : "welding")} the {doorName}...",
						$"{interaction.Performer.ExpensiveName()} starts {(isWelded ? "unwelding" : "welding")} the {doorName}...",
						$"You {(isWelded ? "unweld" : "weld")} the {doorName}.",
						$"{interaction.Performer.ExpensiveName()} {(isWelded ? "unwelds" : "welds")} the {doorName}.",
						ToggleWeld);

					States.Add(DoorProcessingStates.PreventSilently);
					break;

				// Help intent repairs the door
				case Intent.Help:
					if (integrity.PercentageDamaged < 95)
					{
						//Scale repair time based off of percent damage
						//TODO use time multipliers from welder tools
						float time = repairTime - (repairTime * integrity.PercentageDamaged / 200f);

						ToolUtils.ServerUseToolWithActionMessages(interaction, time,
							$"You begin repairing the {doorName}...",
							$"{interaction.Performer.ExpensiveName()} begins to repair the {doorName}...",
							$"You finish repairing the {doorName}.",
							$"{interaction.Performer.ExpensiveName()} repairs the {doorName}.",
							() => RepairDoor(interaction));

						States.Add(DoorProcessingStates.PreventSilently);
					}
					else
					{
						UpdateChatMessage.Send(interaction.Performer, ChatChannel.Examine, ChatModifier.None,
						$"The {doorName} doesn't need repairs.");

						States.Add(DoorProcessingStates.PreventSilently);
					}
					break;
			}
		}

		private void RepairDoor(HandApply interaction)
		{
			integrity.RestoreIntegrity(integrity.initialIntegrity);
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
