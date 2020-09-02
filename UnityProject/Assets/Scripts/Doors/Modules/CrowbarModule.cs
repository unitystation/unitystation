using System.Collections;
using System.Collections.Generic;
using Doors;
using UnityEngine;

public class CrowbarModule : DoorModuleBase
{
	[SerializeField]
	private float pryTime = 4.5f;

	public override ModuleSignal OpenInteraction(HandApply interaction)
	{
		return ModuleSignal.Continue;
	}

    public override ModuleSignal ClosedInteraction(HandApply interaction)
    {
	    //If the door is powered, only allow things that are made to pry doors. If it isn't powered, we let crowbars work.
	    if ((master.PowerCheck() && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.CanPryDoor ))
	        || (!master.PowerCheck() && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Crowbar)))
	    {
		    //allows the jaws of life to pry open doors
		    ToolUtils.ServerUseToolWithActionMessages(interaction, pryTime,
			    "You start prying open the door...",
			    $"{interaction.Performer.ExpensiveName()} starts prying open the door...",
			    $"You force the door open with your {gameObject.ExpensiveName()}!",
			    $"{interaction.Performer.ExpensiveName()} forces the door open!",
			    TryPry);

		    return ModuleSignal.Break;
	    }

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
    }
}
