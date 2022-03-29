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

		public override ModuleSignal OpenInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;
			//If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.
			if ((!master.HasPower ||
			     !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor)) &&
			    (master.HasPower || !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)))
			{
				return ModuleSignal.Continue;
			}

			//allows the jaws of life to pry close doors
			ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
				"You start closing the door...",
				$"{interaction.Performer.ExpensiveName()} starts closing the door...",
				$"",
				$"",
				() => TryPry(interaction));

			return ModuleSignal.Break;

		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;

			//If no power, need crowbar or pry to open doors
			if (master.HasPower == false &&
			    Validations.HasAnyTrait(interaction.HandObject,
				    new []{CommonTraits.Instance.Crowbar, CommonTraits.Instance.CanPryDoor}) == false)
			{
				return ModuleSignal.Continue;
			}

			//If we have power then need pry
			if (master.HasPower &&
			    Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor) == false)
			{
				//No pry so check to see if crowbar can act in power
				if (crowbarRequiresNoPower || Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar) == false)
				{
					return ModuleSignal.Continue;
				}
			}

			if (soundGuid != "")
			{
				SoundManager.StopNetworked(soundGuid);
			}

			soundGuid = Guid.NewGuid().ToString();
			SoundManager.PlayAtPositionAttached(prySound, master.RegisterTile.WorldPositionServer, gameObject, soundGuid);

			//allows the jaws of life to pry open doors
			ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
				"You start prying open the door...",
				$"{interaction.Performer.ExpensiveName()} starts prying open the door...",
				$"",
				$"",
				() => TryPry(interaction), onFailComplete: OnFailPry, playSound: false);

			return ModuleSignal.Break;
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
					Chat.AddActionMsgToChat(interaction.Performer, $"You force the door open with your {interaction.HandObject.ExpensiveName()}!",
						$"{interaction.Performer.ExpensiveName()} forces the door open!");

				}
				else
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"The door does not budge at all!",
						$"{interaction.Performer.ExpensiveName()} Tries to force the door open failing!");
				}
			}
			else if (!master.IsClosed && !master.IsPerformingAction)
			{
				master.PulseTryClose(inforce: true);
			}
		}

		private void OnFailPry()
		{
			SoundManager.StopNetworked(soundGuid);
			soundGuid = "";
		}
	}
}
