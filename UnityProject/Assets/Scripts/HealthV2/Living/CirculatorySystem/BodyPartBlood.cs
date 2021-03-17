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
		[Tooltip("Is this connected to the blood stream at all?")]
		[SerializeField] private bool isBloodCirculated = true;
		/// <summary>
		/// Flag that is true if the body part is connected to the blood stream. If this is false
		/// it will be ignored by circulatory organs (the heart).
		/// </summary>
		public bool isBloodBloodCirculated => isBloodCirculated;

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
		[Tooltip("What blood reagent does expel as waste?")]
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
		[SerializeField]
		[Tooltip("How much blood can this store internally?")]
		public float bloodStoredMax = 0.5f;

		/// <summary>
		/// The amount of reagent this body part needs consume each tick.
		/// </summary>

		[Tooltip("How much blood reagent (eg oxygen) does this need each tick?")]
		[SerializeField] private float bloodReagentConsumed = 0.05f;

		/// <summary>
		/// Used to calculate the point at which the concentration of reagent in the blood is low enough to
		/// damage the organ
		/// </summary>
		private float lowReagentDamageFactor = 3;

		[SerializeField]
		[Tooltip("How much blood reagent does this request per blood pump event?")]
		private float bloodThroughput = 0.15f;
		/// <summary>
		/// The amount of blood ReagentMix this body part will remove and add each blood pump event
		/// Essentially controls the rate of blood flow through the organ
		/// </summary>
		public float BloodThroughput => bloodThroughput;

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[SerializeField]
		[Tooltip("What does this live off?")]
		public Reagent Nutriment;

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
		/// Event that fires when the body part's modifier total changes
		/// </summary>
		public event Action ModifierChange;

		/// <summary>
		/// The total product of all modifiers applied to this body part.  This acts as a multiplier for efficiency,
		/// thus a low TotalModified means the part is less effective, high means it is more effective
		/// </summary>
		[Tooltip("The total amount that modifiers are affecting this part's efficiency by")]
		public float TotalModified = 1;

		/// <summary>
		/// The list of all modifiers currently applied to this part
		/// </summary>
		[Tooltip("All modifiers applied to this")]
		public List<Modifier> AppliedModifiers = new List<Modifier>();

		/// <summary>
		/// Updates the body part's TotalModified value based off of the modifiers being applied to it
		/// </summary>
		public void UpdateMultiplier()
		{
			TotalModified = 1;
			foreach (var Modifier in AppliedModifiers)
			{
				TotalModified *= Mathf.Max(0, Modifier.Multiplier);
			}
			ModifierChange?.Invoke();
		}

		/// <summary>
		/// Adds a new modifier to the body part
		/// </summary>
		public void AddModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = this;
			AppliedModifiers.Add(InModifier);
		}

		/// <summary>
		/// Removes a modifier from the bodypart
		/// </summary>
		public void RemoveModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = null;
			AppliedModifiers.Remove(InModifier);
		}

		#region BloodReagents
		/// ---------------------------
		/// Blood Reagent and Nutriment Methods
		/// ---------------------------
		/// There are two basic reagents that a body part can require to function. The first is 'Blood
		/// Reagents', a common example of this is oxygen. These are essential to the function of the part,
		/// and the part will take damage if there is not enough of it available, commonly because the lungs
		/// are failing. The second is 'Nutriments', these are required for the part to do work, such as
		/// healing, replenishing blood supply, and moving.  When there is not enough Nutriment in the blood
		/// the body part's efficieny will go down, causing sluggish movement and eventually unconsciousness
		/// and death as the heart and lungs fail.

		/// Body parts have an internal pool of blood, their BloodContainer, which the circulatory system
		/// refreshes from the Ready Blood. Body parts draw from this container for their needed reagents. When
		/// the concentration of reagents in the BloodContainer gets to low, the body part will start consuming
		/// less, to its own detriment.

		/// <summary>
		/// Initializes the body part as part of the circulatory system
		/// </summary>
		public void BloodInitialise()
		{
			BloodContainer = this.GetComponent<ReagentContainerBody>();
			if (BloodContainer.ContentsSet == false)
			{
				HealthMaster.CirculatorySystem.FillWithFreshBlood(BloodContainer);
				BloodContainer.ContentsSet = true;
			}
			if(bloodType == null)
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

		protected virtual void ConsumeReagents()
		{
			if (!isBloodReagentConsumed) return;

			// Only get as much oxygen as the appropriate type of blood can give us
			// Useful for bad transplants and if we got injected with orange juice or something
			float maxReagentInBlood = bloodType.GetCapacity(BloodContainer.CurrentReagentMix);
			float availableReagent = BloodContainer.CurrentReagentMix.Subtract(requiredReagent, maxReagentInBlood);

			if (availableReagent <= 0)
			{
				AffectDamage(10f, (int)DamageType.Oxy);
			}
			else if (availableReagent < bloodReagentConsumed * lowReagentDamageFactor)
			{
				//Starts at 1 damage per tick, scales up to 10 as oxygen gets real low
				var damage = Mathf.Min(bloodReagentConsumed * lowReagentDamageFactor / availableReagent, 10f);
				AffectDamage(damage, (int)DamageType.Oxy);
			}
			else
			{
				// Plenty of reagent, heal some damage
				AffectDamage(-1f, (int)DamageType.Oxy);
			}
			availableReagent -= bloodReagentConsumed;
			BloodContainer.CurrentReagentMix.Add(requiredReagent, Mathf.Max(0, availableReagent));

			// Adds waste product (eg CO2) if any, currently always 1:1, could add code to change the ratio
			if (wasteReagent)
			{
				BloodContainer.CurrentReagentMix.Add(wasteReagent, Mathf.Max(bloodReagentConsumed, bloodReagentConsumed + availableReagent));
			}

			// Over saturated blood can be harmful to organs
			//if (BloodContainer[requiredReagent] > maxReagentInBlood)
			//{
				//AffectDamage(1f, (int)DamageType.Brute);
			//}
		}

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

		#endregion

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
		/// in new blood, up to the parts capacity.
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

			var bloodOut = BloodContainer.CurrentReagentMix.TransferTo(HealthMaster.CirculatorySystem.UsedBloodPool, bloodThroughput);

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

	/// <summary>
	/// A modifier that affects the efficiency of a body part.  Modifiers are applied multiplicatively
	/// </summary>
	public class Modifier
	{
		public float Multiplier
		{
			get
			{
				return multiplier;
			}
			set
			{
				if (multiplier != value)
				{
					multiplier = value;
					if (RelatedPart != null)
					{
						RelatedPart.UpdateMultiplier();
					}
				}
			}
		}

		private float multiplier;

		public BodyPart RelatedPart;

	}
}