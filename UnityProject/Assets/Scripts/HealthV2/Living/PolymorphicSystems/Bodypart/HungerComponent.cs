using System;
using System.Linq;
using Chemistry;
using UnityEngine;
using UnityEngine.Serialization;

namespace HealthV2.Living.PolymorphicSystems.Bodypart
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
		// /\ Regeneration = hyper nutriment consumption healing = all body parts?

		public HungerState HungerState = HungerState.Normal;

		[FormerlySerializedAs("ReagentCirculated")] [HideInInspector]
		public ReagentCirculatedComponent reagentCirculatedComponent;

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
				RelatedPart.HealDamage(null, (float) (RelatedPart.Damages[i] / DamageMultiplier), i);
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
	}
}
