using Chemistry;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;

namespace US13.Items.Food
{
	//TODO Needs to be changed over to  medical chemistry Instead
	public class PillStackRemover : Consumable
	{
		public Reagent RADRemover;
		public float Amount = 50;

		public override void TryConsume(GameObject feeder, GameObject eater, bool projectileFed = false)
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
