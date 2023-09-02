using System.Collections.Generic;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2.Living.PolymorphicSystems
{
	public class ReagentSaturationSystem : HealthSystemBase
	{
		private ReagentPoolSystem reagentPoolSystem
		{
			get
			{
				if (_reagentPoolSystem == null)
				{
					_reagentPoolSystem = Base.reagentPoolSystem;
				}

				return _reagentPoolSystem;
			}
		}

		private ReagentPoolSystem _reagentPoolSystem;

		public Dictionary<BloodType, Dictionary<Reagent, ReagentWithBodyParts>> SaturationToConsume =
			new Dictionary<BloodType, Dictionary<Reagent, ReagentWithBodyParts>>();


		public List<SaturationComponent> BodyParts = new  List<SaturationComponent>();


		public override HealthSystemBase CloneThisSystem()
		{
			return new ReagentSaturationSystem();
		}

		public class ReagentWithBodyParts
		{
			public float Percentage;
			public float TotalNeeded;
			public List<SaturationComponent> RelatedBodyParts = new List<SaturationComponent>();
			public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<SaturationComponent>();
			if (newSaturation != null)
			{
				if (BodyParts.Contains(newSaturation) == false)
				{
					BodyParts.Add(newSaturation);
					BodyPartListChange();
				}
			}
		}

		public override void BodyPartRemoved(BodyPart bodyPart)
		{
			var newSaturation =  bodyPart.GetComponent<SaturationComponent>();
			if (newSaturation != null)
			{
				if (BodyParts.Contains(newSaturation))
				{
					BodyParts.Remove(newSaturation);
				}

				BodyPartListChange();
			}
		}

		public void BodyPartListChange()
		{
			SaturationToConsume.Clear();
			foreach (var bodyPart in BodyParts)
			{
				if (bodyPart.isNotBloodReagentConsumed) continue;
				if (bodyPart.bloodType == null) continue;
				if (SaturationToConsume.ContainsKey(bodyPart.bloodType) == false)
				{
					SaturationToConsume[bodyPart.bloodType] =
						new Dictionary<Reagent, ReagentWithBodyParts>();
				}

				if (SaturationToConsume[bodyPart.bloodType].ContainsKey(bodyPart.requiredReagent) == false)
				{
					SaturationToConsume[bodyPart.bloodType][bodyPart.requiredReagent] =
						new ReagentWithBodyParts();
				}

				var requiredReagent = SaturationToConsume[bodyPart.bloodType][bodyPart.requiredReagent];
				requiredReagent.RelatedBodyParts.Add(bodyPart);

				requiredReagent.TotalNeeded += bodyPart.bloodReagentConsumedPercentageb * bodyPart.reagentCirculatedComponent.Throughput;

				requiredReagent.Percentage += bodyPart.bloodReagentConsumedPercentageb;
				requiredReagent.Percentage *= 0.5f;

				if (bodyPart.wasteReagent)
				{
					if (requiredReagent.ReplacesWith.ContainsKey(bodyPart.wasteReagent) == false)
					{
						requiredReagent.ReplacesWith[bodyPart.wasteReagent] =
							new ReagentWithBodyParts();
					}

					requiredReagent.ReplacesWith[bodyPart.wasteReagent].TotalNeeded +=
						bodyPart.bloodReagentConsumedPercentageb * bodyPart.reagentCirculatedComponent.Throughput;
				}
			}
		}


		public override void SystemUpdate()
		{
			float HeartEfficiency = 0;
			foreach (var Heart in reagentPoolSystem.PumpingDevices)
			{
				HeartEfficiency += Heart.CalculateHeartbeat();
			}

			BloodSaturationCalculations(HeartEfficiency);
		}



		public void BloodSaturationCalculations(float HeartEfficiency)
		{
			foreach (var bloodAndValues in SaturationToConsume)
			{
				foreach (var KVP in bloodAndValues.Value)
				{
					var purityMultiplier = 1f;

					var bloodPressure = 1f;

					var percentageBloodPressure = reagentPoolSystem.BloodPool.Total / reagentPoolSystem.StartingBlood;
					if (percentageBloodPressure < 0.75f)
					{
						bloodPressure = percentageBloodPressure / 0.75f;
					}

					if (percentageBloodPressure > 1.25f)
					{
						Base.ChangeBleedStacks(1); //TODO Change to per body part instead
					}

					var percentage = 0f;
					if (reagentPoolSystem.BloodPool.reagents.ContainsKey(bloodAndValues.Key))
					{
						percentage = reagentPoolSystem.BloodPool.GetPercent(bloodAndValues.Key);
					}


					if (percentage < 0.33f)
					{
						purityMultiplier = percentage / 0.33f;
					}

					//Heal if blood saturation consumption is fine, otherwise do damage
					float bloodSaturation = 0;
					float bloodCap = bloodAndValues.Key.GetGasCapacity(Base.reagentPoolSystem.BloodPool, KVP.Key);
					if (bloodCap > 0)
					{
						bloodSaturation = reagentPoolSystem.BloodPool[KVP.Key] / bloodCap;
					}

					bloodSaturation = bloodSaturation * HeartEfficiency *
					                  bloodAndValues.Key.CalculatePercentageBloodPresent(reagentPoolSystem.BloodPool);


					bloodSaturation *= purityMultiplier;
					bloodSaturation *= bloodPressure;


					var Available =
						reagentPoolSystem.BloodPool[KVP.Key] * KVP.Value.Percentage *
						HeartEfficiency; // This is just all -  Stuff nothing to do with saturation!

					// Numbers could use some tweaking, maybe consumption goes down when unconscious?
					reagentPoolSystem.BloodPool.Subtract(KVP.Key, Available);

					// Adds waste product (eg CO2) if any
					foreach (var KVP2 in KVP.Value.ReplacesWith)
					{
						reagentPoolSystem.BloodPool.Add(KVP2.Key, (KVP2.Value.TotalNeeded / KVP.Value.TotalNeeded) * Available);
					}

					var info = bloodAndValues.Key;
					float damage;
					if (bloodSaturation < info.BLOOD_REAGENT_SATURATION_BAD)
					{
						//Deals damage that ramps to 1 as blood saturation levels drop, halved if unconscious
						if (bloodSaturation <= 0)
						{
							damage = 1f;
						}
						else if (bloodSaturation < info.BLOOD_REAGENT_SATURATION_CRITICAL)
						{
							// Arbitrary damage formula, could use anything here
							damage = 1 * (1 - Mathf.Sqrt(bloodSaturation));
						}
						else
						{
							damage = 1;
						}
					}
					else
					{
						damage = -1;
						if (bloodSaturation > info.BLOOD_REAGENT_SATURATION_OKAY)
						{
							damage = -2;
						}
					}

					bloodSaturation = Mathf.Min(bloodSaturation, 1);

					foreach (var bodyPart in KVP.Value.RelatedBodyParts)
					{
						bodyPart.currentBloodSaturation = bloodSaturation;
						if (damage <= 0)
						{
							if (bodyPart.RelatedPart.Oxy  > 0)
							{
								bodyPart.RelatedPart.TakeDamage(null, damage, AttackType.Internal, DamageType.Oxy, DamageSubOrgans: false);
							}
						}
						else
						{
							bodyPart.RelatedPart.TakeDamage(null, damage, AttackType.Internal, DamageType.Oxy, DamageSubOrgans: false);
						}
					}
				}
			}
		}
	}
}