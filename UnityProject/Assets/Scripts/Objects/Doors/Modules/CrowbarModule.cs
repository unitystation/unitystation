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
				$"You force close the door with your {interaction.HandObject.ExpensiveName()}!",
				$"{interaction.Performer.ExpensiveName()} force closes the door!",
				TryPry);

			return ModuleSignal.Break;

		}

		public override ModuleSignal ClosedInteraction(HandApply interaction, HashSet<DoorProcessingStates> States)
		{
			if (interaction == null) return ModuleSignal.Continue;
			//If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.
			if ((!master.HasPower ||
			     !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor)) &&
			    (master.HasPower || !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)))
			{
				return ModuleSignal.Continue;
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
				$"You force the door open with your {interaction.HandObject.ExpensiveName()}!",
				$"{interaction.Performer.ExpensiveName()} forces the door open!",
				TryPry, onFailComplete: OnFailPry, playSound: false);

			return ModuleSignal.Break;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer, HashSet<DoorProcessingStates> States)
		{
			return ModuleSignal.Continue;
		}

		private void TryPry()
		{
			if (master.IsClosed && !master.IsPerformingAction)
			{
				master.TryForceOpen();
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
