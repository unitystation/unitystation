using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OintmentTrigger : PickUpTrigger
{

	public override void Attack(GameObject target, GameObject originator, BodyPartType bodyPart)
	{
		var LHB = target.GetComponent<LivingHealthBehaviour>();
		if (LHB.IsDead)
		{
			return;
		}
		var targetBodyPart = LHB.FindBodyPart(bodyPart);
		if (targetBodyPart.BurnDamage > 0)
		{
			targetBodyPart.HealDamage(40, DamageType.Burn);
		}
	}
}
