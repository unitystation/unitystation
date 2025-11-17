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

		private WeldModule weldModule;

		protected override void Awake()
		{
			base.Awake();

			weldModule = GetComponentInChildren<WeldModule>();
		}

		public override void OpenInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
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
					string[] verbage = {interaction.Performer.ExpensiveName(), interaction.Performer.GetTheirPronoun().Uncapitalize(), interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName.Uncapitalize(),"forcing","closed","force"};
					PryDoor(interaction, false, verbage);
					States.Add(DoorProcessingStates.PreventSilently);
				}

				//If its a crowbar or a tool that can pry doors, attempt to pry closed the door
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) ||
					Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
				{
					string[] verbage = {interaction.Performer.ExpensiveName(), interaction.Performer.GetTheirPronoun().Uncapitalize(), interaction.HandObject.ExpensiveName().Uncapitalize(),"forcing","closed","force"};
					PryDoor(interaction, true, verbage);
					States.Add(DoorProcessingStates.PreventSilently);
				}
			}
		}

		public override void ClosedInteraction(HandApply interaction, ref HashSet<DoorProcessingStates> States)
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
					string[] verbage = {interaction.Performer.ExpensiveName(), interaction.Performer.GetTheirPronoun().Uncapitalize(), interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName.Uncapitalize(),"prying","open","pry"};
					PryDoor(interaction, false, verbage);
					States.Add(DoorProcessingStates.PreventSilently);
				}

				//If its a crowbar or a tool that can pry doors, attempt to pry open the door
				if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) ||
					Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
				{
					string[] verbage = {interaction.Performer.ExpensiveName(), interaction.Performer.GetTheirPronoun().Uncapitalize(), interaction.HandObject.ExpensiveName().Uncapitalize(),"prying","open","pry"};
					PryDoor(interaction, true, verbage);
					States.Add(DoorProcessingStates.PreventSilently);
				}
			}

			return;
		}

		public override void BumpingInteraction(GameObject byPlayer, ref HashSet<DoorProcessingStates> States)
		{
			return;
		}

		private void PryDoor(HandApply interaction, bool useTool, string[] verbage)
		{
			if (useTool == true)
			{
				if(Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor))
				{
					master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.JawsPry);
				}
				else
				{
					master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.ToolPry);
				}

				ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
					$"You start {verbage[3]} the {master.DoorName} {verbage[4]}...",
					$"{verbage[0]} starts {verbage[3]} the {master.DoorName} {verbage[4]}...",
					$"",
					$"",
					() => TryPry(interaction, verbage), onFailComplete: OnFailPry, playSound: false);
			}
			else
			{
				master.SoundController.ServerPlaySound(DoorSoundController.DoorSoundType.HandPry);

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You start {verbage[3]} the {master.DoorName} {verbage[4]}...",
					$"{verbage[0]} starts {verbage[3]} the {master.DoorName} {verbage[4]} with {verbage[1]} {verbage[2]}...");

				var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

				StandardProgressAction.Create(
					cfg,
					() => TryPry(interaction, verbage)
				).ServerStartProgress(master.RegisterTile, pryTime, interaction.Performer);
			}
		}

		private void TryPry(HandApply interaction, string[] verbage)
		{
			//Refuse if door is in motion
			if (master.IsPerformingAction) return;

			//Refuse if its a crowbar and the door has power
			if ((crowbarRequiresNoPower && master.HasPower) &&
				(Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) == false))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"The {master.DoorName} does not budge at all!",
				$"{verbage[0]} tries to {verbage[5]} the {master.DoorName} {verbage[4]} and fails!");
				return;
			}

			//Try to close the door if open
			if (master.IsClosed == false)
			{
				master.TryForceClose();
				return;
			}

			//Try to open the door if closed
			if (master.IsClosed == true)
			{
				if (master.TryForceOpen())
				{
					Chat.AddActionMsgToChat(interaction.Performer,
						$"You pry the {master.DoorName} open with your {verbage[2]}!",
						$"{verbage[0]} pries the {master.DoorName} open with {verbage[1]} {verbage[2]}!");
				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"The {master.DoorName} does not budge at all!",
						$"{verbage[0]} tries to pry the {master.DoorName} open and fails!");
				}
			}
		}

		private void OnFailPry()
		{
			master.SoundController.StopSound();
		}
	}
}