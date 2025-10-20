using System;
using AddressableReferences;
using UnityEngine;
using System.Collections.Generic;
using Systems.Antagonists;

namespace Doors.Modules
{
	public class CrowbarModule : DoorModuleBase
	{
		[SerializeField] [Tooltip("Base time it takes to pry this door.")]
		private float pryTime = 4.5f; //TODO calculate time with a multiplier from the tool itself

		[SerializeField] [Tooltip("Can you crowbar pry the door when there no power")]
		private bool crowbarRequiresNoPower = true;

		private string doorName;
		private WeldModule weldModule;

		protected override void Awake()
		{
			base.Awake();

			doorName = transform.parent.gameObject.ExpensiveName();

			weldModule = GetComponentInChildren<WeldModule>();
		}

		public override void OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			//Require help intent to pry doors
			if (interaction is { Intent: Intent.Help })
			{
				//If its hands that can pry doors, attempt to open the door
				//TODO Currently hard coded to not allow larva, when prying with hands is moved to body parts just have larva not have the pry ability and update this
				if (interaction.HandObject == null
					&& interaction.PerformerPlayerScript.PlayerTypeSettings.CanPryDoorsWithHands &&
					(interaction.PerformerPlayerScript.TryGetComponent<AlienPlayer>(out var alienPlayer) == false ||
					 alienPlayer.IsLarva == false))
				{
					PryDoor(interaction, false, true);
					States.Add(DoorProcessingStates.PreventSilently);
				}

				//If its a crowbar or a tool that can pry doors, attempt to pry closed the door
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) ||
					Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
				{
					PryDoor(interaction, true, true);
					States.Add(DoorProcessingStates.PreventSilently);
				}
			}
		}

		public override void ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			//Require the Help Intent and the door to be unwelded, can't even try to pry a welded door
			if (interaction is { Intent: Intent.Help } && weldModule.IsWelded == false)
			{
				//If its hands that can pry doors, attempt to pry the door
				//TODO Currently hard coded to not allow larva, when prying with hands is moved to body parts just have larva not have the pry ability and update this
				if (interaction.HandObject == null
					&& interaction.PerformerPlayerScript.PlayerTypeSettings.CanPryDoorsWithHands &&
					(interaction.PerformerPlayerScript.TryGetComponent<AlienPlayer>(out var alienPlayer) == false ||
					 alienPlayer.IsLarva == false))
				{
					PryDoor(interaction, false, false);
					States.Add(DoorProcessingStates.PreventSilently);
				}

				//If its a crowbar or a tool that can pry doors, attempt to pry open the door
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) ||
					Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
				{
					PryDoor(interaction, true, false);
					States.Add(DoorProcessingStates.PreventSilently);
				}
			}

			return;
		}

		public override void BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return;
		}

		private void PryDoor(HandApply interaction, bool useTool, bool isClosing)
		{

			if (useTool == true)
			{
				master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.ToolPry);
				ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
					$"You start {(isClosing ? "forcing" : "prying")} the {doorName} {(isClosing ? "closed" : "open")}...",
					$"{interaction.Performer.ExpensiveName()} starts {(isClosing ? "forcing" : "prying")} the {doorName} {(isClosing ? "closed" : "open")}...",
					$"",
					$"",
					() => TryPry(interaction, useTool, isClosing), onFailComplete: OnFailPry, playSound: false);
			}

			else if (useTool == false)
			{
				master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.HandPry);
				Chat.AddActionMsgToChat(interaction.Performer,
					$"You start {(isClosing ? "forcing" : "prying")} the {doorName} {(isClosing ? "closed" : "open")}...",
					$"{interaction.Performer.ExpensiveName()} starts {(isClosing ? "forcing" : "prying")} the {doorName} {(isClosing ? "closed" : "open")} with its {interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName}...");

				var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

				StandardProgressAction.Create(
					cfg,
					() => TryPry(interaction, useTool, isClosing)
				).ServerStartProgress(master.RegisterTile, pryTime, interaction.Performer);
			}
		}

		private void TryPry(HandApply interaction, bool useTool, bool isClosing)
		{
			//Refuse if door is in motion
			if (master.IsPerformingAction) return;

			//Refuse if its a crowbar and the door has power
			else if ((crowbarRequiresNoPower && master.HasPower) &&
				(Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) == false))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"The {doorName} does not budge at all!",
				$"{interaction.Performer.ExpensiveName()} tries to {(isClosing ? "force" : "pry")}  the {doorName} {(isClosing ? "closed" : "open")} and fails!");
				return;
			}

			//Try to close the door if open
			else if (master.IsClosed == false)
			{
				master.TryForceClose();
				return;
			}

			//Try to open the door if closed
			else if (master.IsClosed == true)
			{
				if (master.TryForceOpen())
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You pry the {doorName} open with your {(useTool ? interaction.HandObject.ExpensiveName() : interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName)}!",
						$"{interaction.Performer.ExpensiveName()} pries the {doorName} open{(useTool ? "" : " with its " + interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName)}!");
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"The {doorName} does not budge at all!",
						$"{interaction.Performer.ExpensiveName()} tries to pry the {doorName} open and fails!");
				}

			}
		}

		private void OnFailPry()
		{
			master.SoundController.StopSound();
		}
	}
}