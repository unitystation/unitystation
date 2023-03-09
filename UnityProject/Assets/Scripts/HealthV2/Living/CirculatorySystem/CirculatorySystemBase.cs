using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using Chemistry.Components;
using HealthV2.Living.CirculatorySystem;
using Items.Implants.Organs;
using NaughtyAttributes;

namespace HealthV2
{
	[RequireComponent(typeof(LivingHealthMasterBase))]
	public class CirculatorySystemBase : MonoBehaviour, IAreaReactionBase
	{
		public List<MetabolismReaction> ALLMetabolismReactions = new List<MetabolismReaction>(); //TOOD Move somewhere static maybe
		public List<MetabolismReaction> MetabolismReactions { get; } = new();

		public Dictionary<MetabolismReaction, List<BodyPart>> PrecalculatedMetabolismReactions = new  Dictionary<MetabolismReaction, List<BodyPart>>();

		[SerializeField]
		[Required("Must have a blood type in a circulatory system.")]
		private BloodType bloodType = null;
		public BloodType BloodType => bloodType;
		public ReagentMix BloodPool;
		private Chemistry.Reagent CirculatedReagent => bloodType.CirculatedReagent;

		[SerializeField]
		[Required("Inital injecton of blood on player spawn")]
		public int StartingBlood = 500;

		[SerializeField]
		[Required("Need to know our limits for how much blood we have and what not.")]
		private CirculatoryInfo bloodInfo = null;
		public CirculatoryInfo BloodInfo => bloodInfo;

		private LivingHealthMasterBase healthMaster;

		public void SetBloodType(BloodType inBloodType)
		{
			bloodType = inBloodType;
		}

		private void Awake()
		{
			healthMaster = GetComponent<LivingHealthMasterBase>();
			AddFreshBlood(BloodPool, StartingBlood);
		}

		///<summary>
		/// Adds a volume of blood along with the maximum normal reagents
		///</summary>
		public void AddFreshBlood(ReagentMix bloodPool, float amount)
		{
			// Currently only does blood and required reagents, should at nutriments and other common gases
			var bloodToAdd = new ReagentMix(BloodType, amount);
			bloodToAdd.Add(CirculatedReagent, bloodType.GetSpareGasCapacity(bloodToAdd));
			bloodPool.Add(bloodToAdd);
		}

		public void Bleed(float amount)
		{
			var bloodLoss = new ReagentMix();
			BloodPool.TransferTo(bloodLoss, amount);
			MatrixManager.ReagentReact(bloodLoss, healthMaster.gameObject.RegisterTile().WorldPositionServer);
		}

		public List<Heart> Hearts = new List<Heart>();


		public class ReagentWithBodyParts
		{
			public float Percentage;
			public float TotalNeeded;
			public List<BodyPart> RelatedBodyParts = new List<BodyPart>();
			public Dictionary<Reagent, ReagentWithBodyParts> ReplacesWith = new Dictionary<Reagent, ReagentWithBodyParts>();
		}

		//TODO  Heavy stuff is put on here and Done in bulk, If you want to add custom logic do it within components that inherit from BodyPartFunctionality
		//If it's laggy and there's a bunch of systems, maybe it might be worth making a generic version of this

		//loop Player chemistry set
		//Then get by tag
		//then apply effect
		public Dictionary<Reagent, ReagentWithBodyParts> NutrimentToConsume = new Dictionary<Reagent, ReagentWithBodyParts>();

		public Dictionary<BloodType, Dictionary<Reagent,ReagentWithBodyParts>> SaturationToConsume = new Dictionary<BloodType, Dictionary<Reagent,ReagentWithBodyParts>>();

		public Dictionary<Reagent, ReagentWithBodyParts> Toxicity = new Dictionary<Reagent, ReagentWithBodyParts>();


		public void BodyPartListChange()
		{
			NutrimentToConsume.Clear();
			SaturationToConsume.Clear();
			Toxicity.Clear();
			PrecalculatedMetabolismReactions.Clear();

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false)
				{
					continue;
				}

				if (bodyPart.IsBloodReagentConsumed)
				{
					if (SaturationToConsume.ContainsKey(bodyPart.bloodType) == false)
					{
						SaturationToConsume[bodyPart.bloodType] = new Dictionary<Reagent, ReagentWithBodyParts>();
					}

					if (SaturationToConsume[bodyPart.bloodType].ContainsKey(bodyPart.requiredReagent) == false)
					{
						SaturationToConsume[bodyPart.bloodType][bodyPart.requiredReagent] = new ReagentWithBodyParts();
					}

					var requiredReagent = SaturationToConsume[bodyPart.bloodType][bodyPart.requiredReagent];
					requiredReagent.RelatedBodyParts.Add(bodyPart);

					requiredReagent.TotalNeeded += bodyPart.bloodReagentConsumedPercentageb * bodyPart.BloodThroughput;

					requiredReagent.Percentage += bodyPart.bloodReagentConsumedPercentageb;
					requiredReagent.Percentage *= 0.5f;

					if (bodyPart.wasteReagent)
					{
						if (requiredReagent.ReplacesWith.ContainsKey(bodyPart.wasteReagent) == false)
						{
							requiredReagent.ReplacesWith[bodyPart.wasteReagent] = new ReagentWithBodyParts();

						}
						requiredReagent.ReplacesWith[bodyPart.wasteReagent].TotalNeeded += bodyPart.bloodReagentConsumedPercentageb * bodyPart.BloodThroughput;
					}
				}


				if (bodyPart.HasNaturalToxicity) //Could be better
				{
					if (Toxicity.ContainsKey(bodyPart.NaturalToxinReagent) == false)
					{
						Toxicity[bodyPart.NaturalToxinReagent] = new ReagentWithBodyParts();
					}

					Toxicity[bodyPart.NaturalToxinReagent].RelatedBodyParts.Add(bodyPart);
					Toxicity[bodyPart.NaturalToxinReagent].TotalNeeded += bodyPart.ToxinGeneration * bodyPart.BloodThroughput;
				}

				if (bodyPart.CanGetHungry)
				{
					if (NutrimentToConsume.ContainsKey(bodyPart.Nutriment) == false)
					{
						NutrimentToConsume[bodyPart.Nutriment] = new ReagentWithBodyParts();
					}
					NutrimentToConsume[bodyPart.Nutriment].RelatedBodyParts.Add(bodyPart);
					NutrimentToConsume[bodyPart.Nutriment].TotalNeeded += bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput;
				}
			}


			foreach (var MR in ALLMetabolismReactions)
			{
				foreach (var bodyPart in healthMaster.BodyPartList)
				{
					if (bodyPart.IsBloodCirculated == false) continue;

					if (bodyPart.ItemAttributes.HasAllTraits(MR.InternalAllRequired) &&
					    bodyPart.ItemAttributes.HasAnyTrait(MR.InternalBlacklist) == false)
					{
						if (PrecalculatedMetabolismReactions.ContainsKey(MR) == false)
						{
							PrecalculatedMetabolismReactions[MR] = new List<BodyPart>();
						}
						PrecalculatedMetabolismReactions[MR].Add(bodyPart);
					}
				}
			}
		}

		public void BloodUpdate()
		{
			float HeartEfficiency = 0;
			foreach (var Heart in Hearts)
			{
				HeartEfficiency += Heart.CalculateHeartbeat();
			}

			Heartbeat(HeartEfficiency);

		}


		public void Heartbeat(float HeartEfficiency)
		{
			NutrimentCalculation(HeartEfficiency);
			BloodSaturationCalculations(HeartEfficiency);
			MetaboliseReactions();
			ToxinGeneration(HeartEfficiency); //Could be better
		}

		public void BloodSaturationCalculations(float HeartEfficiency)
		{

			foreach (var bloodAndValues in SaturationToConsume)
			{
				foreach (var KVP in bloodAndValues.Value)
				{
					var purityMultiplier = 1f;

					var bloodPressure = 1f;

					var percentageBloodPressure = BloodPool.Total  / StartingBlood;
					if (percentageBloodPressure < 0.75f)
					{
						bloodPressure = percentageBloodPressure / 0.75f;
					}

					if (percentageBloodPressure > 1.25f)
					{
						healthMaster.ChangeBleedStacks(1); //TODO Change to per body part instead

					}

					var percentage = 0f;
					if (BloodPool.reagents.ContainsKey(bloodAndValues.Key))
					{
						percentage = BloodPool.GetPercent(bloodAndValues.Key);
					}


					if (percentage < 0.33f)
					{
						purityMultiplier = percentage / 0.33f;
					}

					//Heal if blood saturation consumption is fine, otherwise do damage
					float bloodSaturation = 0;
					float bloodCap = bloodAndValues.Key.GetGasCapacity(BloodPool, KVP.Key);
					if (bloodCap > 0)
					{
						bloodSaturation = BloodPool[KVP.Key] / bloodCap;
					}

					bloodSaturation = bloodSaturation * HeartEfficiency * bloodAndValues.Key.CalculatePercentageBloodPresent(BloodPool);


					bloodSaturation *= purityMultiplier;
					bloodSaturation *= bloodPressure;




					var Available = BloodPool[KVP.Key] * KVP.Value.Percentage * HeartEfficiency; // This is just all -  Stuff nothing to do with saturation!

					// Numbers could use some tweaking, maybe consumption goes down when unconscious?
					BloodPool.Subtract(KVP.Key,Available);

					// Adds waste product (eg CO2) if any
					foreach (var KVP2 in KVP.Value.ReplacesWith)
					{
						BloodPool.Add(KVP2.Key, (KVP2.Value.TotalNeeded / KVP.Value.TotalNeeded) * Available);
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
						bodyPart.TakeDamage(null, damage,  AttackType.Internal, DamageType.Oxy,
							false, true, default, default,
							default, false);
					}
				}
			}
		}



		public void NutrimentCalculation(float HeartEfficiency)
		{
			foreach (var KVP in NutrimentToConsume)
			{
				float Needed = KVP.Value.TotalNeeded;
				foreach (var bodyPart in KVP.Value.RelatedBodyParts)
				{
					if (bodyPart.TotalDamageWithoutOxy > 0)
					{
						Needed -= bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput;
						Needed += bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput * bodyPart.HealingNutrimentMultiplier;
					}
				}


				var AvailablePercentage = BloodPool[KVP.Key] / Needed;
				var Effective = Mathf.Min(HeartEfficiency, AvailablePercentage);

				var Amount = Needed * Effective;
				BloodPool.Remove(KVP.Key, Amount);
				foreach (var bodyPart in KVP.Value.RelatedBodyParts)
				{
					if (Effective > 0.1f)
					{
						if (bodyPart.HungerModifier.Multiplier != 1)
						{
							bodyPart.HungerModifier.Multiplier = 1f;
						}

						bodyPart.HungerState = HungerState.Normal;

						if (bodyPart.TotalDamageWithoutOxy > 0)
						{
							var Total = bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput * bodyPart.HealingNutrimentMultiplier * Effective;
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


		public void ToxinGeneration(float HeartEfficiency)
		{
			float Multiplier = HeartEfficiency;
			if (HeartEfficiency == 0)
			{
				Multiplier = 0.0025f;
			}

			foreach (var KVP in Toxicity)
			{
				BloodPool.Add(KVP.Key, KVP.Value.TotalNeeded * Multiplier);
			}
		}


		public void MetaboliseReactions()
		{
			MetabolismReactions.Clear();

			foreach (var Reaction in PrecalculatedMetabolismReactions)
			{
				Reaction.Key.Apply(this, BloodPool);
			}

			foreach (var Reaction in MetabolismReactions)
			{
				float ProcessingAmount = 0;
				foreach (var bodyPart in PrecalculatedMetabolismReactions[Reaction]) //TODO maybe lag? Alternative?
				{
					ProcessingAmount += bodyPart.ReagentMetabolism * bodyPart.BloodThroughput * bodyPart.CurrentBloodSaturation * Mathf.Max(0.10f, bodyPart.TotalModified);
				}

				if (ProcessingAmount == 0) continue;

				Reaction.React(PrecalculatedMetabolismReactions[Reaction], BloodPool, ProcessingAmount);
			}
		}

		//The cause of world hunger
		public void InitialiseHunger(float numberOfMinutesBeforeHunger)
		{
			var TotalBloodThroughput = 0f;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.CanGetHungry == false) continue;
				TotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var ConsumptionPerFlowSecond = (1f / 60f) / TotalBloodThroughput;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.CanGetHungry == false) continue;
				bodyPart.PassiveConsumptionNutriment = ConsumptionPerFlowSecond;
			}
			//numberOfMinutesBeforeHunger
			var Stomachs = healthMaster.GetStomachs();;

			var MinutesAvailable = 0f;

			foreach (var Stomach in Stomachs)
			{
				foreach (var bodyFat in Stomach.BodyFats)
				{
					MinutesAvailable += bodyFat.AbsorbedAmount;
				}
			}

			var  Bymultiply = numberOfMinutesBeforeHunger / MinutesAvailable;

			Bymultiply *= (1 + UnityEngine.Random.Range(-0.25f, 0.25f));

			foreach (var Stomach in Stomachs)
			{
				foreach (var bodyFat in Stomach.BodyFats)
				{
					bodyFat.AbsorbedAmount *= Bymultiply;
				}
			}
		}

		public void InitialiseToxGeneration(float TotalToxinGenerationPerSecond)
		{
			var TotalBloodThroughput = 0f;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.HasNaturalToxicity == false) continue;
				TotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var ToxinFlowPerOne = TotalToxinGenerationPerSecond / TotalBloodThroughput;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.HasNaturalToxicity == false) continue;
				bodyPart.ToxinGeneration = ToxinFlowPerOne;
			}
		}

		public void InitialiseMetabolism(PlayerHealthData RaceBodypart)
		{

			var InternalTotalBloodThroughput = 0f;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.DamageContributesToOverallHealth) continue;
				InternalTotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var InternalMetabolismFlowPerOne = RaceBodypart.Base.InternalMetabolismPerSecond / InternalTotalBloodThroughput;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.DamageContributesToOverallHealth) continue;
				bodyPart.ReagentMetabolism = InternalMetabolismFlowPerOne;
			}



			var ExternalTotalBloodThroughput = 0f;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.DamageContributesToOverallHealth == false) continue;
				ExternalTotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var MetabolismFlowPerOne =  RaceBodypart.Base.ExternalMetabolismPerSecond / ExternalTotalBloodThroughput;

			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.IsBloodCirculated == false) continue;
				if (bodyPart.DamageContributesToOverallHealth == false) continue;
				bodyPart.ReagentMetabolism = MetabolismFlowPerOne;
			}
		}


		public void InitialiseDefaults(PlayerHealthData HealthData)
		{
			foreach (var bodyPart in healthMaster.BodyPartList)
			{
				if (bodyPart.Nutriment == null)
				{
					bodyPart.Nutriment = HealthData.Base.BodyNutriment;
				}

				if (bodyPart.NaturalToxinReagent == null)
				{
					bodyPart.NaturalToxinReagent = HealthData.Base.BodyNaturalToxinReagent;
				}
			}
		}


	}


	public enum BleedingState
	{
		None,
		VeryLow,
		Low,
		Medium,
		High,
		UhOh
	}
}
