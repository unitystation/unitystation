using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
	//TODO Needs to be changed over to  medical chemistry Instead
	public class PillStackRemover : Consumable
	{
		public float StackPercentageRemove = 50;

		public override void TryConsume(GameObject feeder, GameObject eater)
		{
			var health = eater.GetComponent<LivingHealthBehaviour>();
			health.RadiationStacks *= StackPercentageRemove / 100f;
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
