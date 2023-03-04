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

		[SerializeField] private float gauzeApplyTime = 1.2f;

		public override bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject) == false) return false;
			return interaction.Intent == Intent.Help;
		}

		public override void ServerPerformInteraction(HandApply interaction)
		{
			var LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			if (LHB.BleedStacks > 0)
			{
				StandardProgressAction action = StandardProgressAction.Create(new StandardProgressActionConfig(StandardProgressActionType.CPR, true),
					() =>
					{
						LHB.SetBleedStacks(LHB.BleedStacks > 2 ? LHB.BleedStacks / 2 : 0);
						stackable.ServerConsume(1);
						Chat.AddActionMsgToChat(interaction.Performer, $"{interaction.PerformerPlayerScript.visibleName} applies the gauze.");
						if (HasTrauma(LHB)) HealTrauma(LHB);
					});
				action.ServerStartProgress(interaction.PerformerPlayerScript.gameObject.AssumedWorldPosServer(), gauzeApplyTime, interaction.Performer);
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