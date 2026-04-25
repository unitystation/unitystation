using Chemistry;
using NaughtyAttributes;
using US13.HealthV2.Living.Metabolism;
using US13.Items.Implants.Organs;

namespace US13.HealthV2.Living.PolymorphicSystems.Hunger.HungerCalculationMethods
{
	public class NutrimentInBlood : IHungerCalculation
	{
		[BoxGroup("Thresholds")] public float NutrimentThresholdForStarving = 10f;
		[BoxGroup("Thresholds")] public float NutrimentThresholdForMalnourished = 15f;
		[BoxGroup("Thresholds")] public float NutrimentThresholdForHunger = 20f;
		[BoxGroup("Thresholds")] public float NutrimentThresholdForNormal = 40f;

		[BoxGroup("Consumption")] public float NutrimentConsumedPerTick = 0.01f;
		[BoxGroup("Consumption")] public float healingPerTick = 0.0085f;

		[BoxGroup("Setup")] public float StartingNutritionAmountInBlood = 30f;
		[BoxGroup("Setup")] public ReagentMix StartingNutrimentInStomachs = new ReagentMix();

		public void Initialize(LivingHealthMasterBase creatureHealth, HungerSystem hungerSystem)
		{
			hungerSystem.ApplyStatusAffectsEffects = true;
			creatureHealth.reagentPoolSystem.BloodPool.Add(hungerSystem.BodyNutriment, StartingNutritionAmountInBlood);
			foreach (Stomach stomach in creatureHealth.GetStomachs())
			{
				stomach.StomachContents.Add(StartingNutrimentInStomachs);
				foreach (var fat in stomach.BodyFats)
				{
					fat.SetAbsorbedAmount(fat.MinuteStoreMaxAmount);
				}
			}
		}

		public HungerState CalculateHungerState(LivingHealthMasterBase creatureHealth, HungerSystem hungerSystem)
		{
			var currentNutriment = 0f;
			foreach (var nurimentNbodyParts in hungerSystem.NutrimentToConsume)
			{
				creatureHealth.reagentPoolSystem.BloodPool.Remove(nurimentNbodyParts.Key, NutrimentConsumedPerTick);
				currentNutriment += creatureHealth.reagentPoolSystem.BloodPool.GetAmountOfReagent(nurimentNbodyParts.Key);
				if (currentNutriment <= 0) continue;
				foreach (var bodyPart in nurimentNbodyParts.Value.RelatedBodyParts)
				{
					bodyPart.NutrimentHeal(healingPerTick);
				}
			}

			foreach (var stomach in creatureHealth.GetStomachs())
			{
				if (stomach.StomachContents.SpareCapacity <= stomach.StomachIsConsideredFullWhenSpareCapacityIsLessThan)
				{
					return HungerState.Full;
				}
			}

			if (currentNutriment < NutrimentThresholdForStarving)
			{
				return HungerState.Starving;
			}
			if (currentNutriment < NutrimentThresholdForMalnourished)
			{
				return HungerState.Malnourished;
			}
			if (currentNutriment < NutrimentThresholdForHunger)
			{
				return HungerState.Hungry;
			}
			return currentNutriment < NutrimentThresholdForNormal ? HungerState.Normal : HungerState.Full;
		}
	}
}