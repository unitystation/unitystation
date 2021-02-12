using UnityEngine;

namespace Doors.Modules
{
	public class CrowbarModule : DoorModuleBase
	{
		[SerializeField][Tooltip("Base time it takes to pry this door.")]
		private float pryTime = 4.5f; //TODO calculate time with a multiplier from the tool itself

		public override ModuleSignal OpenInteraction(HandApply interaction)
		{
			//If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.
			if ((!master.HasPower ||
			     !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor)) &&
			    (master.HasPower || !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)))
			{
				return ModuleSignal.Continue;
			}

			//allows the jaws of life to pry close doors
			ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
				"You start prying open the door...",
				$"{interaction.Performer.ExpensiveName()} starts prying close the door...",
				$"You force the door open with your {interaction.HandObject.ExpensiveName()}!",
				$"{interaction.Performer.ExpensiveName()} forces the door close!",
				TryPry);

			return ModuleSignal.Break;

		}

		public override ModuleSignal ClosedInteraction(HandApply interaction)
		{
			//If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.
			if ((!master.HasPower ||
			     !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor)) &&
			    (master.HasPower || !Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)))
			{
				return ModuleSignal.Continue;
			}

			//allows the jaws of life to pry open doors
			ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
				"You start prying open the door...",
				$"{interaction.Performer.ExpensiveName()} starts prying open the door...",
				$"You force the door open with your {interaction.HandObject.ExpensiveName()}!",
				$"{interaction.Performer.ExpensiveName()} forces the door open!",
				TryPry);

			return ModuleSignal.Break;
		}

		public override ModuleSignal BumpingInteraction(GameObject byPlayer)
		{
			return ModuleSignal.Continue;
		}

		public override bool CanDoorStateChange()
		{
			return true;
		}

		public void TryPry()
		{
			if (master.IsClosed && !master.IsPerformingAction)
			{
				master.TryForceOpen();
			}
			else if (!master.IsClosed && !master.IsPerformingAction)
			{
				master.TryClose(force: true);
			}
		}
	}
}
