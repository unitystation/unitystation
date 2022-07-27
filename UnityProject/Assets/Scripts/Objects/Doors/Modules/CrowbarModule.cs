using System;
using AddressableReferences;
using UnityEngine;
using System.Collections.Generic;

namespace Doors.Modules
{
	public class CrowbarModule : DoorModuleBase
	{
		[SerializeField][Tooltip("Base time it takes to pry this door.")]
		private float pryTime = 4.5f; //TODO calculate time with a multiplier from the tool itself

		[SerializeField]
		private AddressableAudioSource prySound = null;

		[SerializeField]
		[Tooltip("Can you crowbar pry the door when there no power")]
		private bool crowbarRequiresNoPower = true;

		private string soundGuid = "";

		private string doorName;

		protected override void Awake()
		{
			base.Awake();

			doorName = transform.parent.gameObject.ExpensiveName();
		}

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;
			//If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) || Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
			{
				if ((crowbarRequiresNoPower && master.HasPower) && (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) == false) )
				{
					return ModuleSignal.Continue;
				}

				ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
					$"You start closing the {doorName}...",
					$"{interaction.Performer.ExpensiveName()} starts closing the {doorName}...",
					$"",
					$"",
					() => TryPry(interaction));

				return ModuleSignal.Break;
			}


			return ModuleSignal.Continue;

			//allows the jaws of life to pry close doors
		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;

			if (interaction.HandObject == null
			    && interaction.PerformerPlayerScript.PlayerTypeSettings.CanPryDoorsWithHands)
			{
				PryDoor(interaction, false);
				return ModuleSignal.Break;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) ||
			    Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar))
			{
				if ((crowbarRequiresNoPower && master.HasPower) && (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) == false))
				{
					return ModuleSignal.Continue;
				}

				PryDoor(interaction);
				return ModuleSignal.Break;
			}

			return ModuleSignal.Continue;
		}

		private void PryDoor(HandApply interaction, bool useTool = true)
		{
			if (soundGuid != "")
			{
				SoundManager.StopNetworked(soundGuid);
			}

			soundGuid = Guid.NewGuid().ToString();
			SoundManager.PlayAtPositionAttached(prySound, master.RegisterTile.WorldPositionServer, gameObject, soundGuid);

			if (useTool)
			{
				//allows the jaws of life to pry open doors
				ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
					$"You start prying open the {doorName}...",
					$"{interaction.Performer.ExpensiveName()} starts prying open the {doorName}...",
					$"",
					$"",
					() => TryPry(interaction), onFailComplete: OnFailPry, playSound: false);

				return;
			}

			var handName = interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName;

			Chat.AddActionMsgToChat(interaction.Performer, $"You start prying open the {doorName}...",
				$"{interaction.Performer.ExpensiveName()} starts prying open the {doorName} with its {handName}...");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => TryPryHand(interaction)
			).ServerStartProgress(master.RegisterTile, pryTime, interaction.Performer);
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		private void TryPry(HandApply interaction)
		{
			if (master.IsClosed && !master.IsPerformingAction)
			{
				if (master.TryForceOpen())
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You force the {doorName} open with your {interaction.HandObject.ExpensiveName()}!",
						$"{interaction.Performer.ExpensiveName()} forces the {doorName} open!");

				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"The {doorName} does not budge at all!",
						$"{interaction.Performer.ExpensiveName()} tries to force the {doorName} open, and fails!");
				}
			}
			else if (!master.IsClosed && !master.IsPerformingAction)
			{
				master.PulseTryClose(inforce: true);
			}
		}

		private void TryPryHand(HandApply interaction)
		{
			if (master.IsClosed == false && master.IsPerformingAction == false)
			{
				master.PulseTryClose(inforce: true);
				return;
			}

			if (master.IsClosed == false || master.IsPerformingAction) return;

			var handName = interaction.PerformerPlayerScript.PlayerTypeSettings.PryHandName;

			if (master.TryForceOpen())
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"You force the {doorName} open with your {handName}!",
					$"{interaction.Performer.ExpensiveName()} forces the {doorName} open with its {handName}!");

			}
			else
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"The {doorName} does not budge at all!",
					$"{interaction.Performer.ExpensiveName()} tries to force the {doorName} open and fails!");
			}
		}

		private void OnFailPry()
		{
			SoundManager.StopNetworked(soundGuid);
			soundGuid = "";
		}
	}
}
