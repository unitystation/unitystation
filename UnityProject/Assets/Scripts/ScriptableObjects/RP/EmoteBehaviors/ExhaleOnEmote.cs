using System.Collections.Generic;
using Chemistry;
using HealthV2;
using Items.Implants.Organs;
using Systems.Atmospherics;
using UnityEngine;

namespace ScriptableObjects.RP.EmoteBehaviors
{
	public class ExhaleOnEmote : IEmoteBehavior
	{
		public float efficiency = 0.25f;

		public void Behave(GameObject actor)
		{
			if (actor == null || actor.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			var gas = GasMix.GetEnvironmentalGasMixForObject(actor.GetUniversalObjectPhysics());
			var lungs = GetLungs(health);
			if (lungs == null || gas == null) return;
			ReagentMix availableBlood = health.reagentPoolSystem.BloodPool
				.Take((health.reagentPoolSystem.BloodPool.Total * efficiency) / 2f);
			lungs.PickRandom()?.BreatheOut(gas, availableBlood);
			health.reagentPoolSystem.RegenBloodPool.Add(availableBlood.Take(availableBlood.Total));
		}

		public static List<Lungs> GetLungs(LivingHealthMasterBase health)
		{
			var result = new List<Lungs>();
			foreach (var part in health.BodyPartList)
			{
				if (part.TryGetComponent<Lungs>(out var t))
				{
					if(t) result.Add(t);
				}
			}
			return result;
		}
	}
}