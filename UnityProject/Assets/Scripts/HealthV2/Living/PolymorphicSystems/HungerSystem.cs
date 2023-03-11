using System.Collections.Generic;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class HungerSystem : HealthSystemBase
	{
		public Dictionary<Reagent, ReagentWithBodyParts> NutrimentToConsume =
			new Dictionary<Reagent, ReagentWithBodyParts>();

		public List<HungerComponent> BodyParts;

		private ReagentPoolSystem _reagentPoolSystem;

		public override void InIt()
		{
			_reagentPoolSystem = Base.reagentPoolSystem; //idk Shouldn't change
		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<HungerComponent>();
			if (component != null)
			{
				BodyParts.Add(component);
				BodyPartListChange();
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<HungerComponent>();
			if (component != null)
			{
				if (BodyParts.Contains(component))
				{
					BodyParts.Remove(component);
				}

				BodyPartListChange();
			}
		}

		public void BodyPartListChange()
		{
			NutrimentToConsume.Clear();

			foreach (var bodyPart in BodyParts)
			{
				if (NutrimentToConsume.ContainsKey(bodyPart.Nutriment) == false)
				{
					NutrimentToConsume[bodyPart.Nutriment] = new ReagentWithBodyParts();
				}

				NutrimentToConsume[bodyPart.Nutriment].RelatedBodyParts.Add(bodyPart);
				NutrimentToConsume[bodyPart.Nutriment].TotalNeeded +=
					bodyPart.PassiveConsumptionNutriment * bodyPart.reagentCirculatedComponent.Throughput;
			}
		}

		public override void SystemUpdate()
		{
			float HeartEfficiency = 0;
			foreach (var Heart in _reagentPoolSystem.PumpingDevices)
			{
				HeartEfficiency += Heart.CalculateHeartbeat();
			}

			NutrimentCalculation(HeartEfficiency);
		}

		public void NutrimentCalculation(float HeartEfficiency)
		{
			foreach (var KVP in NutrimentToConsume)
			{
				float Needed = KVP.Value.TotalNeeded;
				foreach (var bodyPart in KVP.Value.RelatedBodyParts)
				{
					if (bodyPart.RelatedPart.TotalDamageWithoutOxy > 0)
					{
						Needed -= bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput;
						Needed += bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput *
						          bodyPart.HealingNutrimentMultiplier;
					}
				}


				var AvailablePercentage = _reagentPoolSystem.BloodPool[KVP.Key] / Needed;
				var Effective = Mathf.Min(HeartEfficiency, AvailablePercentage);

				var Amount = Needed * Effective;
				_reagentPoolSystem.BloodPool.Remove(KVP.Key, Amount);
				foreach (var bodyPart in KVP.Value.RelatedBodyParts)
				{
					if (Effective > 0.1f)
					{
						if (bodyPart.HungerModifier.Multiplier != 1)
						{
							bodyPart.HungerModifier.Multiplier = 1f;
						}

						bodyPart.HungerState = HungerState.Normal;

						if (bodyPart.RelatedPart.TotalDamageWithoutOxy > 0)
						{
							var Total = bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput *
							            bodyPart.HealingNutrimentMultiplier * Effective;
							bodyPart.NutrimentHeal(Total);
						}
					}
					else
					{
						if (bodyPart.HungerModifier.Multiplier != 0.5f)
						{
							bodyPart.HungerModifier.Multiplier = 0.5f;
						}

						bodyPart.HungerState = HungerState.Starving; //TODO Can optimise by setting the main hunger thing
					}
				}
			}
		}

		public override HealthSystemBase CloneThisSystem()
		{
			return new HungerSystem();
		}


		public class ReagentWithBodyParts
		{
			public float Percentage;
			public float TotalNeeded;
			public List<HungerComponent> RelatedBodyParts = new List<HungerComponent>();
			public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
		}
	}
}