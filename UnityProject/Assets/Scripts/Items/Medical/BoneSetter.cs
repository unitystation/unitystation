using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HealthV2;

namespace Items.Medical
{
	public class BoneSetter : HealsTheLiving
	{
		public override void ServerPerformInteraction(HandApply interaction)
		{
			LivingHealthMasterBase LHB = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
			PushPull pull = interaction.Performer.gameObject.GetComponent<PushPull>();
			if (!pull.IsPullingSomething) return;

			BodyPart partToHeal = null;

			foreach (BodyPart bodyPart in LHB.BodyPartList)
			{
				if (bodyPart.BodyPartType == interaction.TargetBodyPart)
				{
					partToHeal = bodyPart;
					break;
				}
			}

			if (partToHeal.CurrentBluntDamageLevel <= TraumaDamageLevel.NONE)
			{
				Chat.AddExamineMsg(interaction.Performer, "There's nothing broken to fix!");
			}
			else if (partToHeal.CurrentBluntDamageLevel == TraumaDamageLevel.SMALL)
			{
				void ProgressComplete()
				{
					SetBone(partToHeal);
				}

				StandardProgressAction.Create(ProgressConfig, ProgressComplete)
					.ServerStartProgress(interaction.Performer.RegisterTile(), timeTakenToHeal, interaction.Performer);
			}
		}

		public void SetBone(BodyPart bodyPart, TraumaDamageLevel level = TraumaDamageLevel.NONE)
		{
			bodyPart.HealTraumaticDamage(TraumaticDamageTypes.BLUNT, level);
			if (level == TraumaDamageLevel.NONE) bodyPart.AnnounceJointHealEvent();
		}
	}
}
