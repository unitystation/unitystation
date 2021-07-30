using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HealthV2;

namespace Items
{
	//TODO Needs to be changed over to  medical chemistry Instead
	public class PillToxHeale : Consumable
	{
		public int damageRemove = 50;

		public override void TryConsume(GameObject feeder, GameObject eater)
		{
			var health = eater.GetComponent<LivingHealthMasterBase>();
			foreach (var container in health.RootBodyPartContainers)
			{
				health.HealDamage(gameObject, damageRemove, DamageType.Radiation, container.BodyPartType);
			}
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
