using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;

namespace US13.HealthV2.Living.PolymorphicSystems.Hunger.HungerCalculationMethods
{
	/// <summary>
	/// The default hunger calculation method for upstream UnityStation.
	/// Will heal and consume more nutrients for damaged body parts.
	/// </summary>
	public class DefaultUnityStationHungerCalculation : IHungerCalculation
	{
		public HungerState CalculateHungerState(LivingHealthMasterBase creatureHealth, HungerSystem hungerSystem)
		{
			float heartEfficiency = 0;
			foreach (var heart in creatureHealth.reagentPoolSystem.PumpingDevices)
			{
				heartEfficiency += heart.CalculateHeartbeat();
			}
			NutrimentCalculation(heartEfficiency, hungerSystem.NutrimentToConsume, creatureHealth);
			return CheckHungerStateOnAll(hungerSystem.BodyParts);
		}

		/// <summary>
		/// Determines the overall hunger state of the creature by iterating all body parts.
		/// - Returns Full immediately if any body part is Full.
		/// - Otherwise returns the most severe state found (highest enum value).
		/// - Stops early if Starving is reached.
		/// </summary>
		public HungerState CheckHungerStateOnAll(List<HungerComponent> bodyParts)
		{
			var state = HungerState.Full;
			foreach (var bodyPart in bodyParts)
			{
				// If any body part is Full, the creature is considered Full overall.
				if (bodyPart.HungerState == HungerState.Full)
				{
					state = HungerState.Full;
					break;
				}

				// Escalate to the worst (highest int value) hunger state seen so far.
				if ((int)bodyPart.HungerState > (int)state)
				{
					state = bodyPart.HungerState;
					if (state == HungerState.Starving)
					{
						break; // Starving is the worst possible state; no need to check further.
					}
				}
			}

			return state;
		}

		/// <summary>
        /// Core nutriment consumption logic. Called every tick with the current combined
        /// heart efficiency (0–1+ range, where 1 = full circulation).
        ///
        /// For each required nutriment reagent:
        ///  1. Calculate total needed this tick, adding a healing bonus for damaged body parts.
        ///  2. Determine what fraction of the needed amount is actually available in the blood pool.
        ///  3. Clamp delivery to the lower of heart efficiency and availability (the bottleneck).
        ///  4. Remove the delivered amount from the blood pool.
        ///  5. For each body part:
        ///     - If delivery > 10%: mark as Normal, reset speed modifier to 1×, and apply
        ///       healing if the part is damaged.
        ///     - If delivery ≤ 10%: mark as Starving and halve the body part's speed modifier.
        /// </summary>
        public void NutrimentCalculation(float HeartEfficiency, Dictionary<Reagent, HungerSystem.ReagentWithBodyParts> NutrimentToConsume, LivingHealthMasterBase health)
        {
            foreach (var KVP in NutrimentToConsume)
            {
                float needed = KVP.Value.TotalNeeded;

                // Increase demand for damaged body parts that need extra nutriment to heal.
                foreach (var bodyPart in KVP.Value.RelatedBodyParts)
                {
                    if (bodyPart.RelatedPart.TotalDamageWithoutOxy > 0)
                    {
                        // Remove the normal amount and substitute the healing-boosted amount.
                        needed -= bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput;
                        needed += bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput
                                  * bodyPart.HealingNutrimentMultiplier;
                    }
                }

                // What fraction of demand can the blood pool cover this tick?
                var availablePercentage = health.reagentPoolSystem.BloodPool[KVP.Key] / needed;

                // Effective delivery is capped by whichever is the limiting factor:
                // heart output or blood pool availability.
                var effective = Mathf.Min(HeartEfficiency, availablePercentage);

                // Remove the delivered amount from the circulating blood pool.
                var amount = needed * effective;
                health.reagentPoolSystem.BloodPool.Remove(KVP.Key, amount);

                // Update each body part based on how much nutriment it actually received.
                foreach (var bodyPart in KVP.Value.RelatedBodyParts)
                {
                    if (effective > 0.1f)
                    {
                        // Sufficient nutriment delivered — body part is functioning normally.
                        if (Mathf.Approximately(bodyPart.HungerModifier.Multiplier, 1) == false)
                        {
                            bodyPart.HungerModifier.Multiplier = 1f; // Restore normal speed.
                        }

                        bodyPart.HungerState = HungerState.Normal;

                        // If the part is damaged, apply a healing tick proportional to delivery.
                        if (bodyPart.RelatedPart.TotalDamageWithoutOxy > 0)
                        {
	                        // PassiveConsumptionNutriment is baseline metabolic "burn rate" per unit of blood flow per second.
	                        // BloodThroughput is How much blood flows through this body part per second.
	                        // HealingNutrimentMultiplier: when the part is damaged, it demands more nutriment than normal. This is the cost side of healing; it represents the extra metabolic work of repairing tissue. 0.0006 x 2.0 = 0.0012 nutriment/sec while healing.
	                        // Effective: The fraction of demanded nutriment that was actually delivered this tick, capped by both heart efficiency and blood pool availability.
                            var total = bodyPart.PassiveConsumptionNutriment
                                        * bodyPart.BloodThroughput
                                        * bodyPart.HealingNutrimentMultiplier
                                        * effective;
                            bodyPart.NutrimentHeal(total);
                        }
                    }
                    else
                    {
                        // Insufficient nutriment, body part is starving.
                        if (Mathf.Approximately(bodyPart.HungerModifier.Multiplier, 0.5f) == false)
                        {
                            bodyPart.HungerModifier.Multiplier = 0.5f; // Halve movement/action speed.
                        }

                        bodyPart.HungerState = HungerState.Starving;
                    }
                }
            }
        }
	}
}