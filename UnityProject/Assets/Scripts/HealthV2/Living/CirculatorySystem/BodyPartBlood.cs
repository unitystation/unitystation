using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	/// Handles the body part's usage of the blood stream.
	public partial class BodyPart
	{
		/// <summary>
		/// Modifier that multiplicatively reduces the efficiency of the body part based on damage
		/// </summary>
		[Tooltip("Modifier to reduce efficiency when the character gets hungry")] [NonSerialized]
		private Modifier HungerModifier = new Modifier();

		[HorizontalLine] [Tooltip("Is this connected to the blood stream at all?")] [SerializeField]
		private bool isBloodCirculated = true;


		[SerializeField] private bool CanGetHungry = true;

		[SerializeField] private bool HasNaturalToxicity = true;

		/// <summary>
		/// Flag that is true if the body part is connected to the blood stream. If this is false
		/// it will be ignored by circulatory organs (the heart).
		/// </summary>
		[SerializeField] private bool IsBloodCirculated => isBloodCirculated;

		[Tooltip("Does this consume reagents from its blood?")]
		[SerializeField] private bool isBloodReagentConsumed = false;

		/// <summary>
		/// Flag that is true if the body part consumes reagents (eg oxygen) from the blood
		/// </summary>
		[SerializeField] private bool IsBloodReagentConsumed => isBloodReagentConsumed;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What type of blood does this body part work with?")] [NonSerialized]
		[SerializeField] private BloodType bloodType = null;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What blood reagent does this use?")]
		[SerializeField] private Chemistry.Reagent requiredReagent;

		/// <summary>
		/// The reagent that the body part expels as waste, eg co2
		/// </summary>
		[Tooltip("What reagent does this expel as waste?")]
		[SerializeField] private Chemistry.Reagent wasteReagent;

		/// <summary>
		/// The amount (in moles) of required reagent (eg oxygen) this body part needs consume each tick.
		/// </summary>
		[Tooltip("What percentage per update of oxygen*(Required reagent) is consumed")]
		[SerializeField] private float bloodReagentConsumedPercentageb = 0.5f;

		[Tooltip("How much blood reagent does this request per blood pump event?")]
		[SerializeField] private float bloodThroughput = 5f; //This will need to be reworked when heartrate gets finished

		/// <summary>
		/// The amount of blood ReagentMix this body part will remove and add each blood pump event
		/// Essentially controls the rate of blood flow through the organ
		/// </summary>
		[SerializeField] private float BloodThroughput => bloodThroughput;


		[SerializeField] private float currentBloodSaturation = 0;

		[SerializeField] private float CurrentBloodSaturation => currentBloodSaturation;

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[Tooltip("What does this live off?")]
		[SerializeField] private Reagent Nutriment;

		/// <summary>
		/// The amount of of nutriment to consumed each tick as part of passive metabolism
		/// </summary>
		[NonSerialized] //Automatically generated runtime
		[SerializeField] private float PassiveConsumptionNutriment = 0.00012f;



		[Tooltip("How many metabolic reactions can happen inside of this body part Per tick per 1u of blood flow ")]
		[SerializeField] private float ReagentMetabolism = 0.2f;


		/// <summary>
		/// The amount of of nutriment to consume in order to perform work, eg heal damage or replenish blood supply
		/// </summary>
		[Tooltip("How much more nutriment does it consume each Second")]
		[SerializeField] private float HealingNutrimentMultiplier = 2f;
		// /\ Regeneration = hyper nutriment consumption healing = all body parts?



		public HashSet<MetabolismReaction> MetabolismReactions = new HashSet<MetabolismReaction>();

		/// <summary>
		/// The National toxins that the body part makes ( give these to the liver to filter ) E.G Toxin
		/// </summary>
		[Tooltip("What reagent does this expel as waste?")]
		[SerializeField] private Chemistry.Reagent NaturalToxinReagent;

		[Tooltip("How much natural toxicity does this body part generate Per tick per 1u of blood flow ")]
		[SerializeField] private float ToxinGeneration = 0.0002f;


		[SerializeField] private HungerState HungerState = HungerState.Normal;


		/// <summary>
		/// Heals damage caused by sources other than lack of blood reagent
		/// </summary>
		/// <param name="amount">Amount to heal</param>
		public void NutrimentHeal(double amount)
		{
			double DamageMultiplier = TotalDamageWithoutOxy / amount;

			for (int i = 0; i < Damages.Length; i++)
			{
				if ((int) DamageType.Oxy == i) continue;
				HealDamage(null, (float) (Damages[i] / DamageMultiplier), i);
			}
		}
	}
}