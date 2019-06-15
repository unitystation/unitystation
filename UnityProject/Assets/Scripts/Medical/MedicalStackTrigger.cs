using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main medical stack component. For bruise packs and ointments.
/// </summary>
public class MedicalStackTrigger : Pickupable
{
	public DamageType healType;
	private bool isSelfHealing;
	private int amount = 6; //TODO: move into some stack component (metal sheets, ores, etc)

	public override void Attack(GameObject target, GameObject originator, BodyPartType bodyPart)
	{
		var LHB = target.GetComponent<LivingHealthBehaviour>();
		if (LHB.IsDead)
		{
			return;
		}
		var targetBodyPart = LHB.FindBodyPart(bodyPart);
		if (targetBodyPart.GetDamageValue(healType) > 0)
		{
			if(target != originator)
			{
				ApplyHeal(targetBodyPart);
			}
			else
			{
				SelfHeal(originator, targetBodyPart);
			}
		}
	}

	private void ApplyHeal(BodyPartBehaviour targetBodyPart)
	{
		targetBodyPart.HealDamage(40, healType);
		amount--;
		if(amount == 0)
		{
			DisappearObject();
		}
	}

	private void SelfHeal(GameObject originator, BodyPartBehaviour targetBodyPart)
	{
		if (!isSelfHealing)
		{
			var progressFinishAction = new FinishProgressAction(
				reason =>
				{
					if (reason == FinishProgressAction.FinishReason.INTERRUPTED)
					{
						isSelfHealing = false;
					}
					else if (reason == FinishProgressAction.FinishReason.COMPLETED)
					{
						ApplyHeal(targetBodyPart);
						isSelfHealing = false;
					}
				}
			);
			isSelfHealing = true;
			UIManager.ProgressBar.StartProgress(originator.transform.position.RoundToInt(), 5f, progressFinishAction, originator);
		}
	}

}
