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
		[Tooltip("Modifier to reduce efficiency when the character gets hungry")]
		[NonSerialized] public Modifier HungerModifier = new Modifier();

		[HorizontalLine]
		[Tooltip("Is this connected to the blood stream at all?")] [SerializeField]
		private bool isBloodCirculated = true;

		public bool CanGetHungry = true;

		/// <summary>
		/// Flag that is true if the body part is connected to the blood stream. If this is false
		/// it will be ignored by circulatory organs (the heart).
		/// </summary>
		public bool IsBloodCirculated => isBloodCirculated;

		[Tooltip("Does this consume reagents from its blood?")] [SerializeField]
		private bool isBloodReagentConsumed = false;

		/// <summary>
		/// Flag that is true if the body part consumes reagents (eg oxygen) from the blood
		/// </summary>
		public bool IsBloodReagentConsumed => isBloodReagentConsumed;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What type of blood does this body part work with?")]
		[NonSerialized]	public BloodType bloodType = null;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What blood reagent does this use?")]
		public Chemistry.Reagent requiredReagent;

		/// <summary>
		/// The reagent that the body part expels as waste, eg co2
		/// </summary>
		[Tooltip("What reagent does this expel as waste?")]
		public Chemistry.Reagent wasteReagent;

		/// <summary>
		/// The part's internal working set of the body's blood. This is the limit of the blood that the part can
		/// interact with at any given time.  It is refreshed by blood pump events
		/// </summary>
		[Tooltip("The part's internal blood pool")]
		public ReagentContainerBody BloodContainer = null;

		/// <summary>
		/// The maximum size of the Blood Container
		/// </summary>
		public float BloodStoredMax => BloodContainer.MaxCapacity;

		/// <summary>
		/// The amount (in moles) of required reagent (eg oxygen) this body part needs consume each tick.
		/// </summary>
		[Tooltip("How much (in moles) blood reagent (eg oxygen) does this need each tick? For every 1u of blood flow")] [SerializeField]
		private float bloodReagentConsumed = 0.00002f;

		[Tooltip("How much blood reagent does this request per blood pump event?")] [SerializeField]
		private float bloodThroughput = 5f; //This will need to be reworked when heartrate gets finished

		/// <summary>
		/// The amount of blood ReagentMix this body part will remove and add each blood pump event
		/// Essentially controls the rate of blood flow through the organ
		/// </summary>
		public float BloodThroughput => bloodThroughput;

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[Tooltip("What does this live off?")] [SerializeField]
		public Reagent Nutriment;

		/// <summary>
		/// The amount of of nutriment to consumed each tick as part of passive metabolism
		/// </summary>
		[Tooltip("How much nutriment does this passively consume to each tick? For every 1u of blood flow")]
		public float PassiveConsumptionNutriment = 0.0005f;

		/// <summary>
		/// The amount of of nutriment to consume in order to perform work, eg heal damage or replenish blood supply
		/// </summary>
		[Tooltip("How much nutriment does this consume to perform work? For every 1u of blood flow")]
		public float ConsumptionNutriment = 0.002f;

		[Tooltip("How many metabolic reactions can happen inside of this body part Per tick per 1u of blood flow ")]
		public float ReagentMetabolism = 0.2f;

		public HashSet<MetabolismReaction> MetabolismReactions = new HashSet<MetabolismReaction>();

		/// <summary>
		/// The National toxins that the body part makes ( give these to the liver to filter ) E.G Toxin
		/// </summary>
		[Tooltip("What reagent does this expel as waste?")]
		public Chemistry.Reagent NaturalToxinReagent;

		[Tooltip("How much natural toxicity does this body part generate Per tick per 1u of blood flow ")]
		public float ToxinGeneration = 0.002f;


		public HungerState HungerState = HungerState.Normal;

		/// <summary>
		/// Initializes the body part as part of the circulatory system
		/// </summary>
		public void BloodInitialise()
		{
			BloodContainer = this.GetComponent<ReagentContainerBody>();
			if (BloodContainer.ContentsSet == false)
			{
				if (isBloodCirculated)
				{
					HealthMaster.CirculatorySystem.ReadyBloodPool.TransferTo(BloodContainer.CurrentReagentMix,
						BloodStoredMax);
					//BloodContainer.CurrentReagentMix.Add(Nutriment, 0.01f);
				}

				BloodContainer.ContentsSet = true;
			}

			if (bloodType == null)
			{
				bloodType = HealthMaster.CirculatorySystem.BloodType;
			}

			AddModifier(HungerModifier);
		}

		/// <summary>
		/// Handles the body part's use of blood for each tick
		/// </summary>
		protected virtual void BloodUpdate()
		{
			if (isBloodCirculated == false) return;
			ConsumeReagents();
			if (CanGetHungry)
			{
				ConsumeNutriments();
			}

			MetaboliseReactions();
			NaturalToxicity();
			//Assuming it's changed in this update since none of them use the Inbuilt functions
			BloodContainer.OnReagentMixChanged?.Invoke();
			BloodContainer.ReagentsChanged();
		}

		protected virtual void NaturalToxicity()
		{
			BloodContainer.CurrentReagentMix.Add(NaturalToxinReagent, ToxinGeneration * BloodThroughput);
		}

		protected virtual void MetaboliseReactions()
		{
			if (MetabolismReactions.Count == 0) return;
			float ReagentsProcessed = (ReagentMetabolism * bloodThroughput * TotalModified) / MetabolismReactions.Count;
			foreach (var Reaction in MetabolismReactions)
			{
				Reaction.React(this, BloodContainer.CurrentReagentMix, ReagentsProcessed);
			}

			MetabolismReactions.Clear();
		}

		/// <summary>
		/// Handles the body part's consumption of required reagents (eg oxygen)
		/// </summary>
		protected virtual void ConsumeReagents()
		{

			//Heal if blood saturation consumption is fine, otherwise do damage
			float bloodSaturation = 0;
			float bloodCap = bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix);
			if (bloodCap > 0)
			{
				float foreignCap = bloodType.GetGasCapacityForeign(BloodContainer.CurrentReagentMix);
				var ratioNativeBlood = bloodCap / (bloodCap + foreignCap);
				bloodSaturation = BloodContainer[requiredReagent] * ratioNativeBlood / bloodCap;
			}

			// Numbers could use some tweaking, maybe consumption goes down when unconscious?
			if (!isBloodReagentConsumed) return;

			float consumed = BloodContainer.CurrentReagentMix.Subtract(requiredReagent, bloodReagentConsumed * bloodThroughput);

			// Adds waste product (eg CO2) if any, currently always 1:1, could add code to change the ratio
			if (wasteReagent)
			{
				BloodContainer.CurrentReagentMix.Add(wasteReagent, consumed);
			}



			var info = HealthMaster.CirculatorySystem.BloodInfo;
			float damage;
			if (HealthMaster.CirculatorySystem.ReadyBloodPool.Total < info.BLOOD_CRITICAL)
			{
				//If we reach critical, the organism will very quickly accumulate damage.
				//I'm picking 5f arbitarily, change if necessary
				damage = 5f;
			}
			else if (HealthMaster.CirculatorySystem.ReadyBloodPool.Total < info.BLOOD_BAD)
			{
				//There's not enough blood in the body to sustain itself
				damage = 1f;
			}
			else if (bloodSaturation < info.BLOOD_REAGENT_SATURATION_BAD)
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
			else if (bloodSaturation > 2)
			{
				//There is more oxygen in the organ than the blood can hold
				//Blood might be oversaturated, we might have the wrong blood, maybe do something here
				damage = 0;
			}
			else
			{
				if (bloodSaturation > info.BLOOD_REAGENT_SATURATION_OKAY)
				{
					OxyHeal(BloodContainer.CurrentReagentMix,
						BloodContainer[requiredReagent] * (bloodSaturation - info.BLOOD_REAGENT_SATURATION_OKAY));
				}

				//We already consumed some earlier as well
				damage = -1;
			}

			AffectDamage(damage, (int) DamageType.Oxy);
		}

		/// <summary>
		/// Heals damage caused by lack of blood reagent by consume reagent
		/// </summary>
		/// <param name="reagentMix">Reagent mix to consume reagent from</param>
		/// <param name="amount">Amount to consume</param>
		public void OxyHeal(ReagentMix reagentMix, float amount)
		{
			if (Oxy <= 0) return;
			var toConsume = Mathf.Min(amount, Oxy * bloodReagentConsumed * bloodThroughput);
			AffectDamage(-reagentMix.Subtract(requiredReagent, toConsume) / bloodReagentConsumed * bloodThroughput, (int) DamageType.Oxy);
		}

		/// <summary>
		/// Handles the body part's consumption of required nutriments
		/// </summary>
		protected virtual void ConsumeNutriments()
		{
			float availableNutriment = BloodContainer.CurrentReagentMix.Subtract(Nutriment, Single.MaxValue);

			if (availableNutriment > PassiveConsumptionNutriment * bloodThroughput)
			{
				availableNutriment -= PassiveConsumptionNutriment * bloodThroughput;
				if (TotalDamageWithoutOxy > 0)
				{
					float toConsume = Mathf.Min(ConsumptionNutriment * bloodThroughput, availableNutriment);
					availableNutriment -= toConsume;
					NutrimentHeal(toConsume);
				}

				if (availableNutriment < PassiveConsumptionNutriment * bloodThroughput * 7)
				{
					HungerModifier.Multiplier = 0.75f;
					HungerState = HungerState.Malnourished;
					// Is Hungry
				}
				else
				{
					HungerModifier.Multiplier = 1f;
					HungerState = HungerState.Normal;
				}

				BloodContainer.CurrentReagentMix.Add(Nutriment, availableNutriment);
			}
			else
			{
				HungerModifier.Multiplier = 0.5f;
				HungerState = HungerState.Starving;
				// Is Starving
			}
		}

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

		/// <summary>
		/// This is called whenever blood is pumped through the circulatory system by a heartbeat.
		/// Can happen multiple times if there's multiple hearts. Pushes out old blood and brings
		/// in new blood, up to the part's capacity.
		/// </summary>
		/// <param name="bloodIn">Incoming blood</param>
		/// <returns>Whatever is left over from bloodIn</returns>
		public ReagentMix BloodPumpedEvent(ReagentMix bloodIn)
		{
			//Maybe have a dynamic 50% other blood in this blood
			// if (bloodReagent != requiredReagent)
			// {
			// return HandleWrongBloodReagent(bloodReagent, amountOfBloodReagentPumped);
			// }
			//bloodReagent.Subtract()
			//BloodContainer.Add(bloodReagent);

			//Maybe have damage from high/low blood levels and high blood pressure

			BloodContainer.CurrentReagentMix.TransferTo(HealthMaster.CirculatorySystem.UsedBloodPool, float.MaxValue);

			if ((BloodContainer.ReagentMixTotal + bloodIn.Total) > BloodContainer.MaxCapacity)
			{
				float BloodToTake = BloodContainer.MaxCapacity - BloodContainer.ReagentMixTotal;
				bloodIn.TransferTo(BloodContainer.CurrentReagentMix, BloodToTake);
			}
			else
			{
				bloodIn.TransferTo(BloodContainer.CurrentReagentMix, bloodIn.Total);
			}

			BloodContainer.OnReagentMixChanged?.Invoke();
			BloodContainer.ReagentsChanged();
			BloodWasPumped();
			return bloodIn;
		}

		public virtual void BloodWasPumped()
		{
			foreach (var organ in OrganList)
			{
				organ.BloodWasPumped();
			}
		}

		/// <summary>
		/// Called when the implant receives the wrong reagent in the blood pumped too it.
		/// Returns the amount of blood reagent that remains after the pump event, in case it uses any of it.
		/// For example, maybe an organ is damaged by the wrong reagent.
		/// </summary>
		/// <param name="bloodReagent"></param>
		/// <param name="amount"></param>
		/// <returns></returns>
		public virtual float HandleWrongBloodReagent(Chemistry.Reagent bloodReagent, float amount)
		{
			return amount;
		}
	}
}