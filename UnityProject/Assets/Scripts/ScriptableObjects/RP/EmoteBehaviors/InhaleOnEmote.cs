using System.Collections.Generic;
using Chemistry;
using HealthV2;
using Items.Implants.Organs;
using Systems.Atmospherics;
using UnityEngine;

namespace ScriptableObjects.RP.EmoteBehaviors
{
	public class InhaleOnEmote : IEmoteBehavior
	{
		public float Efficiency = 1.25f;

		public void Behave(GameObject actor)
		{
			if (actor == null || actor.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			var gas = GasMix.GetEnvironmentalGasMixForObject(actor.GetUniversalObjectPhysics());
			var lungs = GetLungs(health);
			if (lungs == null || gas == null) return;
			foreach (var lung in lungs)
			{
				ReagentMix availableBlood =
					health.reagentPoolSystem.BloodPool.Take(
						(health.reagentPoolSystem.BloodPool.Total * Efficiency) / 2f);
				lung.BreatheIn(gas, availableBlood, Efficiency);
				health.reagentPoolSystem.BloodPool.Add(availableBlood);
			}
		}

		private List<Lungs> GetLungs(LivingHealthMasterBase health)
		{
			var result = new List<Lungs>();
			foreach (var part in health.BodyPartList)
			{
				if (part.TryGetComponent<Lungs>(out var t)) result.Add(t);
			}
			return result;
		}
	}
}