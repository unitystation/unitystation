using HealthV2;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Health.Sickness
{
	public class SicknessHealthDamagerBase : Sickness
	{
		[Range(0f, 45f), SerializeField] private float damageToDo = 2f;
		[Range(0f, 100f), SerializeField] private float chanceToDamage = 50f;
		[SerializeField] private List<BodyPart> specficBodyPartsToTarget = new List<BodyPart>();
		[SerializeField] private AttackType attackType = AttackType.Bio;
		[SerializeField] private DamageType damageType = DamageType.Tox;
		[SerializeField] private bool hasCooldown = false;
		[SerializeField] private float cooldownTime = 24f;

		private bool isOnCooldown = false;


		public override void SicknessBehavior(LivingHealthMasterBase health)
		{
			if (hasCooldown && isOnCooldown) return;
			health.StartCoroutine(Cooldown());
			if (DMMath.Prob(chanceToDamage) == false) return;
			health.BodyPartList.Shuffle();
			if(specficBodyPartsToTarget.Count != 0)
			{
				foreach(var part in health.BodyPartList)
				{
					if (specficBodyPartsToTarget.Contains(part) == false) continue;
					part.TakeDamage(null, damageToDo, attackType, damageType);
					return;
				}
			}
			var bodyPart = health.BodyPartList.PickRandom();
			bodyPart.TakeDamage(null, damageToDo, attackType, damageType);
		}

		private IEnumerator Cooldown()
		{
			isOnCooldown = true;
			yield return WaitFor.Seconds(cooldownTime);
			isOnCooldown = false;
		}
	}
}

