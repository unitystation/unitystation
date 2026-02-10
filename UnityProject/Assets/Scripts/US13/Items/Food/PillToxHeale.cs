using Chemistry;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;

namespace US13.Items.Food
{


	public class PillToxHeale : Consumable
	{
		public Reagent Antitoxin;

		public float HealingAmount = 25f;

		public override void TryConsume(GameObject feeder, GameObject eater, bool projectileFed = false)
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
