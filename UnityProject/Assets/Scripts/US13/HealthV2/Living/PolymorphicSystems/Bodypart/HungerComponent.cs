using System;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.Metabolism;
using US13.HealthV2.Living.PolymorphicSystems.Hunger;

namespace US13.HealthV2.Living.PolymorphicSystems.Bodypart
{
	public class HungerComponent : BodyPartComponentBase<HungerSystem>
	{
		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
		/// </summary>
		[NonSerialized]
		public Modifier HungerModifier = new Modifier();

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[Tooltip("What does this live off?")] [SerializeField]
		public Reagent Nutriment;


		/// <summary>
		/// The amount of of nutriment to consumed each tick as part of passive metabolism
		/// </summary>
		[NonSerialized] //Automatically generated runtime
		public float PassiveConsumptionNutriment = 0.00012f;

		/// <summary>
		/// The amount of of nutriment to consume in order to perform work, eg heal damage or replenish blood supply
		/// </summary>
		[Tooltip("How much more nutriment does it consume each Second")]
		public float HealingNutrimentMultiplier = 2f;

		[Tooltip("How much more nutriment does it Healing each Nutriment")]
		public float ActualHealingNutrimentMultiplier = 5f;
		// /\ Regeneration = hyper nutriment consumption healing = all body parts?

		public HungerState HungerState = HungerState.Normal;

		[FormerlySerializedAs("ReagentCirculated")] [HideInInspector]
		public ReagentCirculatedComponent reagentCirculatedComponent;

		public float FullMultiplier = 1.1f;
		public float NormalMultiplier = 1;
		public float HungaryMultiplier = 1;
		public float MalnourishedMultiplier = 0.90f;
		public float StarvingMultiplier = 0.90f;
		/// <summary>
		/// Heals damage caused by sources other than lack of blood reagent
		/// </summary>
		/// <param name="amount">Amount to heal</param>
		public void NutrimentHeal(double amount)
		{
			double DamageMultiplier = RelatedPart.TotalDamageWithoutOxy / amount;

			for (int i = 0; i < RelatedPart.Damages.Length; i++)
			{
				if ((int) DamageType.Oxy == i) continue;
				var healAmount = (float) (RelatedPart.Damages[i] / DamageMultiplier) * ActualHealingNutrimentMultiplier;
				if (healAmount is Single.NaN or <= 0)
				{
					continue;
				}
				RelatedPart.HealDamage(null, healAmount, i);
			}
		}

		public float BloodThroughput
		{
			get
			{
				if (reagentCirculatedComponent == null) return 1;
				return reagentCirculatedComponent.Throughput;
			}
		}

		public override void Awake()
		{
			base.Awake();
			reagentCirculatedComponent = GetComponent<ReagentCirculatedComponent>();
			RelatedPart.AddModifier(HungerModifier);
		}

		public float GetMultiplierForHungerState(HungerState state)
		{
			switch (state)
			{
				case HungerState.Full:
					return FullMultiplier;
				case HungerState.Normal:
					return NormalMultiplier;
				case HungerState.Hungry:
					return HungaryMultiplier;
				case HungerState.Malnourished:
					return MalnourishedMultiplier;
				case HungerState.Starving:
					return StarvingMultiplier;
				default:
					return NormalMultiplier;
			}
		}
	}
}
