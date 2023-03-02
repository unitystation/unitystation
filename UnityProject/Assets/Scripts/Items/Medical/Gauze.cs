using System;
using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;
using Items;
using Messages.Server.HealthMessages;

namespace Items.Medical
{
	public class Gauze : HealsTheLiving
	{

		public override bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			//can only be applied to LHB
			if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;

			if(interaction.Intent != Intent.Help) return false;

			return true;
		}

		public override void ServerPerformInteraction(HandApply interaction)
		{
			var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (LHB.BleedStacks > 0)
			{
				LHB.ChangeBleedStacks(-2f);
				stackable.ServerConsume(1);
			}
			else if (CheckForBleedingBodyContainers(LHB, interaction))
			{
				RemoveLimbLossBleed(LHB, interaction);
				LHB.ChangeBleedStacks(-2f);
				stackable.ServerConsume(1);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer,
				$"{LHB.playerScript.visibleName}'s {interaction.TargetBodyPart} doesn't seem to be bleeding.");
			}
		}
	}
}