using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Items;
 using Messages.Server.HealthMessages;

public class Gauze : HealsTheLiving
{

    public override void ServerPerformInteraction(HandApply interaction)
    {
        var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
        if (LHB.ZoneHasDamageOf(interaction.TargetBodyPart,healType))
		{
            if(CheckForBleedingLimbs(LHB, interaction))
            {
                RemoveLimbExternalBleeding(LHB, interaction);
                stackable.ServerConsume(1);
            }
		}
    }

    private void RemoveLimbExternalBleeding(LivingHealthMasterBase targetBodyPart, HandApply interaction)
	{
		foreach(var container in targetBodyPart.RootBodyPartContainers)
		{
			if(container.BodyPartType == interaction.TargetBodyPart)
			{
				foreach(BodyPart limb in container.ContainsLimbs)
				{
                    if(limb.IsBleedingExternally)
                    {
                        limb.StopExternalBleeding();
                        if(interaction.Performer.Player().GameObject == interaction.TargetObject.Player().GameObject)
                        {
                            Chat.AddActionMsgToChat(interaction.Performer.gameObject, 
					        $"You stopped your {interaction.TargetObject.Player().Script.visibleName}'s bleeding.",
					        $"{interaction.PerformerPlayerScript.visibleName} stopped their own bleeding from their {interaction.TargetObject.ExpensiveName()}.");
                        }
                        else
                        {
                            Chat.AddActionMsgToChat(interaction.Performer.gameObject, 
					        $"You stopped {interaction.TargetObject.Player().Script.visibleName}'s bleeding.",
				            $"{interaction.PerformerPlayerScript.visibleName} stopped {interaction.TargetObject.Player().Script.visibleName}'s bleeding.");
                        }
                    }
				}
			}
		}
	}

    private bool CheckForBleedingLimbs(LivingHealthMasterBase targetBodyPart, HandApply interaction)
	{
		foreach(var container in targetBodyPart.RootBodyPartContainers)
		{
			if(container.BodyPartType == interaction.TargetBodyPart)
			{
				foreach(BodyPart limb in container.ContainsLimbs)
				{
					if(limb.IsBleedingExternally)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
