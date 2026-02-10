using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Items.Implants.Organs;
using US13.Tilemaps.Behaviours.Meta.Atmospherics.Data;
using Util;

namespace US13.ScriptableObjects.RP.EmoteBehaviors
{
	public class InhaleOnEmote : IEmoteBehavior
	{
		public float Efficiency = 1.25f;

		public void Behave(GameObject actor)
		{
			if (actor == null || actor.TryGetComponent<LivingHealthMasterBase>(out var health) == false) return;
			var gas = GasMix.GetEnvironmentalGasMixForObject(actor.GetUniversalObjectPhysics());
			var lungs = GetLungs(health);
			if (lungs.Count == 0 || gas == null) return;
			foreach (var lung in lungs)
			{
				lung.BreatheIn(gas, Efficiency, false);
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