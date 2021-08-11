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
			foreach (var Stomach in Stomachs)
			{
				Stomach.StomachContents.Add(new ReagentMix(Antitoxin,HealingAmount/Stomachs.Count));
			}
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
