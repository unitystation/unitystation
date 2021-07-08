using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
	//TODO Needs to be changed over to  medical chemistry Instead
	public class PillToxHeale : Consumable
	{
		public float HealingAmount = 25f;

		public override void TryConsume(GameObject feeder, GameObject eater)
		{
			var Health = eater.GetComponent<LivingHealthBehaviour>();
			Health.bloodSystem.ToxinLevel -= HealingAmount;
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
