using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

namespace Items
{
	//TODO Needs to be changed over to  medical chemistry Instead
	public class PillStackRemover : Consumable
	{
		public Reagent RADRemover;
		public float Amount = 50;

		public override void TryConsume(GameObject feeder, GameObject eater)
		{
			var health = eater.GetComponent<LivingHealthMasterBase>();
			var Stomachs = health.GetStomachs();
			foreach (var Stomach in Stomachs)
			{
				Stomach.StomachContents.Add(new ReagentMix(RADRemover,Amount/Stomachs.Count));
			}
			//health.RadiationStacks *= StackPercentageRemove / 100f;
			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
