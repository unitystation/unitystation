﻿using System;
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
		[Tooltip("Is this connected to the blood stream at all?")]
		[SerializeField] private bool isBloodCirculated = true;
		/// <summary>
		/// Flag that is true if the body part is connected to the blood stream. If this is false
		/// it will be ignored by circulatory organs (the heart).
		/// </summary>
		public bool IsBloodCirculated => isBloodCirculated;

		[Tooltip("Does this consume reagents from its blood?")]
		[SerializeField] private bool isBloodReagentConsumed = false;
		/// <summary>
		/// Flag that is true if the body part consumes reagents (eg oxygen) from the blood
		/// </summary>
		public bool IsBloodReagentConsumed => isBloodReagentConsumed;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What type of blood does this body part work with?")]
		[SerializeField] protected BloodType bloodType = null;

		/// <summary>
		/// The reagent that is used by this body part, eg oxygen.
		/// </summary>
		[Tooltip("What blood reagent does this use?")]
		[SerializeField] protected Chemistry.Reagent requiredReagent;

		/// <summary>
		/// The reagent that the body part expels as waste, eg co2
		/// </summary>
		[Tooltip("What reagent does this expel as waste?")]
		[SerializeField] protected Chemistry.Reagent wasteReagent;

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
		[Tooltip("How much (in moles) blood reagent (eg oxygen) does this need each tick?")]
		[SerializeField] private float bloodReagentConsumed = 0.00002f;

		[Tooltip("How much blood reagent does this request per blood pump event?")]
		[SerializeField] private float bloodThroughput = 0.015f; //This will need to be reworked when heartrate gets finished
		/// <summary>
		/// The amount of blood ReagentMix this body part will remove and add each blood pump event
		/// Essentially controls the rate of blood flow through the organ
		/// </summary>
		public float BloodThroughput => bloodThroughput;

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[Tooltip("What does this live off?")]
		[SerializeField] public Reagent Nutriment;

		/// <summary>
		/// The amount of of nutriment to consumed each tick as part of passive metabolism
		/// </summary>
		[Tooltip("How much nutriment does this passively consume to each tick?")]
		public float NutrimentPassiveConsumption = 0.001f;

		/// <summary>
		/// The amount of of nutriment to consume in order to perform work, eg heal damage or replenish blood supply
		/// </summary>
		[Tooltip("How much nutriment does this consume to perform work?")]
		public float NutrimentConsumption = 0.02f;

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
					HealthMaster.CirculatorySystem.ReadyBloodPool.TransferTo(BloodContainer.CurrentReagentMix, BloodStoredMax);
				}
				BloodContainer.ContentsSet = true;
			}
			if (bloodType == null)
			{
				bloodType = HealthMaster.CirculatorySystem.BloodType;
			}
		}

		/// <summary>
		/// Handles the body part's use of blood for each tick
		/// </summary>
		protected virtual void BloodUpdate()
		{
			if (isBloodCirculated == false) return;
			ConsumeReagents();
			ConsumeNutriments();
		}

		/// <summary>
		/// Handles the body part's consumption of required reagents (eg oxygen)
		/// </summary>
		protected virtual void ConsumeReagents()
		{
			// Numbers could use some tweaking, maybe consumption goes down when unconscious?

			if (!isBloodReagentConsumed) return;

			float consumed = BloodContainer.CurrentReagentMix.Subtract(requiredReagent, bloodReagentConsumed);

			// Adds waste product (eg CO2) if any, currently always 1:1, could add code to change the ratio
			if (wasteReagent)
			{
				BloodContainer.CurrentReagentMix.Add(wasteReagent, consumed);
			}

			//Heal if blood saturation post consumption is fine, otherwise do damage
			float bloodSaturation = 0;
			float bloodCap = bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix);
			if (bloodCap > 0)
			{
				float foreignCap = bloodType.GetGasCapacityForeign(BloodContainer.CurrentReagentMix);
				var ratioNativeBlood = bloodCap / (bloodCap + foreignCap);
				bloodSaturation = BloodContainer[requiredReagent] * ratioNativeBlood / bloodCap;
			}
			var info = HealthMaster.CirculatorySystem.BloodInfo;
			float damage;
			if (bloodSaturation < info.BLOOD_REAGENT_SATURATION_BAD)
			{
				//Deals damage that ramps to 10 as blood saturation levels drop, halved if unconscious
				if (bloodSaturation <= 0)
				{
					damage = 10f;
				}
				else if (bloodSaturation < info.BLOOD_REAGENT_SATURATION_CRITICAL)
				{
					// Arbitrary damage formula, could use anything here
					damage = 10 * (1 - Mathf.Sqrt(bloodSaturation));
				}
				else
				{
					damage = 1;
				}
			}
			else if (bloodSaturation > 1)
			{
				//There is more oxygen in the organ than the blood can hold
				//Blood might be oversaturated, we might have the wrong blood, maybe do something here
				damage = 0;
			}
			else
			{
				if (bloodSaturation > info.BLOOD_REAGENT_SATURATION_OKAY)
				{
					OxyHeal(BloodContainer.CurrentReagentMix, BloodContainer[requiredReagent] * (bloodSaturation - info.BLOOD_REAGENT_SATURATION_OKAY));
				}
				//We already consumed some earlier as well
				damage = -1;
			}
			AffectDamage(damage, (int)DamageType.Oxy);
		}

		/// <summary>
		/// Heals damage caused by lack of blood reagent by consume reagent
		/// </summary>
		/// <param name="reagentMix">Reagent mix to consume reagent from</param>
		/// <param name="amount">Amount to consume</param>
		public void OxyHeal(ReagentMix reagentMix, float amount)
		{
			if (Oxy <= 0) return;
			var toConsume = Mathf.Min(amount, Oxy * bloodReagentConsumed);
			AffectDamage(-reagentMix.Subtract(requiredReagent, toConsume) / bloodReagentConsumed, (int)DamageType.Oxy);
		}

		/// <summary>
		/// Handles the body part's consumption of required nutriments
		/// </summary>
		protected virtual void ConsumeNutriments()
		{
			float availableNutriment = BloodContainer.CurrentReagentMix.Subtract(Nutriment, Single.MaxValue);

			if (availableNutriment > NutrimentPassiveConsumption)
			{
				availableNutriment -= NutrimentPassiveConsumption;
				if (TotalDamageWithoutOxy > 0)
				{
					float toConsume = Mathf.Min(NutrimentConsumption, availableNutriment);
					availableNutriment -= toConsume;
					NutrimentHeal(toConsume);
				}
				if (availableNutriment < NutrimentPassiveConsumption * 1000)
				{
					// Is Hungry
				}
				BloodContainer.CurrentReagentMix.Add(Nutriment, availableNutriment);
			}
			else
			{
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
				if ((int)DamageType.Oxy == i) continue;
				HealDamage(null, (float)(Damages[i] / DamageMultiplier), i);
			}
		}

		/// <summary>
		/// This is called whenever blood is pumped through the circulatory system by a heartbeat.
		/// Can happen multiple times if there's multiple hearts. Pushes out old blood and brings
		/// in new blood, up to the part's capacity.
		/// </summary>
		/// <param name="bloodIn">Incoming blood</param>
		/// <returns>Whatever is left over from bloodIn</returns>
		public ReagentMix BloodPumpedEvent(ReagentMix bloodIn, float efficiency)
		{
			//Maybe have a dynamic 50% other blood in this blood
			// if (bloodReagent != requiredReagent)
			// {
			// return HandleWrongBloodReagent(bloodReagent, amountOfBloodReagentPumped);
			// }
			//bloodReagent.Subtract()
			//BloodContainer.Add(bloodReagent);

			//Maybe have damage from high/low blood levels and high blood pressure

			BloodContainer.CurrentReagentMix.TransferTo(HealthMaster.CirculatorySystem.UsedBloodPool, bloodThroughput * efficiency);

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
			return bloodIn;
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
