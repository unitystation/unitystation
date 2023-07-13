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

		public List<HungerComponent> BodyParts = new List<HungerComponent>();

		private BodyAlertManager BodyAlertManager;

		public float NumberOfMinutesBeforeStarving = 30;

		[Tooltip("What does this live off?, Sets all the body parts that don't have a set nutriment")]
		public Reagent BodyNutriment;

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
		/// <summary>
		/// The current hunger state of the creature, currently always returns normal
		/// </summary>
		private HungerState HungerState => CalculateHungerState();

		public HungerState CashedHungerState = HungerState.Normal;

		public HungerState CalculateHungerState()
		{
			var state = HungerState.Full;
			foreach (var bodyPart in BodyParts)
			{
				if (bodyPart.HungerState == HungerState.Full)
				{
					state = HungerState.Full;
					break;
				}

				if ((int) bodyPart.HungerState > (int) state) //TODO Add the other states
				{
					state = bodyPart.HungerState;
					if (state == HungerState.Starving)
					{
						break;
					}
				}
			}

			return state;
		}

		public AlertSO GetAlertSOFromHunger(HungerState HungerStates)
		{
			switch (HungerStates)
			{
				case HungerState.Full:
					return CommonAlertSOs.Instance.Full;
				case HungerState.Starving:
					return CommonAlertSOs.Instance.Starving;
				case HungerState.Malnourished:
					return CommonAlertSOs.Instance.Malnourished;
				case HungerState.Hungry:
					return CommonAlertSOs.Instance.Hungry;
				default:
					return null;
			}
		}

		public override void InIt()
		{
			base.InIt();
			BodyAlertManager = Base.GetComponent<BodyAlertManager>();
		}

		public override void BodyPartAdded(BodyPart bodyPart)
		{
			var component = bodyPart.GetComponent<HungerComponent>();
			if (component != null)
			{
				if (BodyParts.Contains(component) == false)
				{
					BodyParts.Add(component);
					BodyPartListChange();
				}
			}
		}

		public override void StartFresh()
		{
			foreach (var bodyPart in BodyParts)
			{
				if (bodyPart.Nutriment == null)
				{
					bodyPart.Nutriment = BodyNutriment;
				}
			}

			InitialiseHunger(NumberOfMinutesBeforeStarving);
		}

		//The cause of world hunger
		public void InitialiseHunger(float numberOfMinutesBeforeHunger)
		{
			var TotalBloodThroughput = 0f;

			foreach (var bodyPart in BodyParts)
			{
				TotalBloodThroughput += bodyPart.BloodThroughput;
			}

			var ConsumptionPerFlowSecond = (1f / 60f) / TotalBloodThroughput;

			foreach (var bodyPart in BodyParts)
			{
				bodyPart.PassiveConsumptionNutriment = ConsumptionPerFlowSecond;
			}
			//numberOfMinutesBeforeHunger
			var Stomachs = Base.GetStomachs();

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
			BodyPartListChange();
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
			var State = HungerState;
			float HeartEfficiency = 0;
			foreach (var Heart in reagentPoolSystem.PumpingDevices)
			{
				HeartEfficiency += Heart.CalculateHeartbeat();
			}

			NutrimentCalculation(HeartEfficiency);

			//TODO HungerState should properly have a cash optimisation here!!
			if (State != CashedHungerState)
			{
				var old = GetAlertSOFromHunger(CashedHungerState);
				if (old != null)
				{
					BodyAlertManager.UnRegisterAlert(old);
				}

				CashedHungerState = State;

				var newOne = GetAlertSOFromHunger(State);
				if (newOne != null)
				{
					BodyAlertManager?.RegisterAlert(newOne);
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
					if (bodyPart.RelatedPart.TotalDamageWithoutOxy > 0)
					{
						Needed -= bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput;
						Needed += bodyPart.PassiveConsumptionNutriment * bodyPart.BloodThroughput *
						          bodyPart.HealingNutrimentMultiplier;
					}
				}


				var AvailablePercentage = reagentPoolSystem.BloodPool[KVP.Key] / Needed;
				var Effective = Mathf.Min(HeartEfficiency, AvailablePercentage);

				var Amount = Needed * Effective;
				reagentPoolSystem.BloodPool.Remove(KVP.Key, Amount);
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


		[NaughtyAttributes.Button()]
		public void MakeStarving()
		{
			foreach (var KVP in NutrimentToConsume)
			{
				reagentPoolSystem.BloodPool.Remove(KVP.Key, 9999);
			}
			var Stomachs = Base.GetStomachs();

			foreach (var Stomach in Stomachs)
			{
				foreach (var bodyFat in Stomach.BodyFats)
				{
					bodyFat.AbsorbedAmount = 0;
				}
			}
		}


		[NaughtyAttributes.Button()]
		public void MakeHungary()
		{
			var Stomachs = Base.GetStomachs();

			foreach (var Stomach in Stomachs)
			{
				foreach (var bodyFat in Stomach.BodyFats)
				{
					bodyFat.AbsorbedAmount = 4;
				}
			}
		}

		[NaughtyAttributes.Button()]
		public void MakeFull()
		{
			var Stomachs = Base.GetStomachs();

			foreach (var Stomach in Stomachs)
			{
				foreach (var bodyFat in Stomach.BodyFats)
				{
					bodyFat.AbsorbedAmount = bodyFat.MinuteStoreMaxAmount;
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