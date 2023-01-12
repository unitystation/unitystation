using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

namespace Items
{


	public class PillToxHeale : Consumable
	{
		public Reagent Antitoxin;

		public float HealingAmount = 25f;

		public override void TryConsume(GameObject feeder, GameObject eater)
		{
			var Health = eater.GetComponent<LivingHealthMasterBase>();
			var Stomachs = Health.GetStomachs();

			if (Stomachs.Count == 0)
			{
				//No stomachs?!
				return;
			}
			bool success = false;
			foreach (var Stomach in Stomachs)
			{
				if (Stomach.TryAddReagentsToStomach(new ReagentMix(Antitoxin, HealingAmount)) > 0)
				{
					success = true;
					break;
				}
			}

			if (success == true)
			{
				//health.RadiationStacks *= StackPercentageRemove / 100f;
				_ = Despawn.ServerSingle(gameObject);
			}
		}
	}
}
