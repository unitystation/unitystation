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

		[SerializeField]
		[Tooltip("Does this consume reagents from its blood?")]
		private bool isBloodReagentConsumed = false;
		/// <summary>
		/// Flag that is true if the body part consumes reagents (ie oxygen) from the blood
		/// </summary>
		public bool IsBloodReagentConsumed => isBloodReagentConsumed;

		[SerializeField]
		[Tooltip("Is this connected to the blood stream at all?")]
		private bool isBloodReagentCirculated = true;
		/// <summary>
		/// Flag that is true if the body part is connected to the blood stream
		/// </summary>
		public bool IsBloodReagentCirculated => isBloodReagentCirculated;

		/// <summary>
		/// The reagent that is used by this body part, ie oxygen
		/// </summary>
		[SerializeField]
		[Tooltip("What blood reagent does this use?")]
		protected Chemistry.Reagent requiredReagent;

		/// <summary>
		/// The amount of reagent consumed by this body part per blood pump event, can be different
		/// from per tick if multiple hearts are present
		/// </summary>
		[SerializeField]
		[Tooltip("How much blood reagent does this consume per blood pump event?")]
		private float _bloodReagentConsumed = 0.10f;

		/// <summary>
		/// The amount of blood that is checked by this body part per blood pump event
		/// </summary>
		[SerializeField]
		[Tooltip("How much blood reagent does this process per tick?")]
		private float bloodReagentProcessed = 0.15f;

		[SerializeField]
		[Tooltip("How much blood reagent does this store per blood pump event?")]
		private float bloodReagentStoreAmount = 0.01f;

		/// <summary>
		/// The amount of reagent this body part will attempt to store per blood pump event
		/// </summary>
		public float BloodReagentStoreAmount => bloodReagentStoreAmount;

		/// <summary>
		/// The maximum amount of reagent this body part can store
		/// </summary>
		[SerializeField]
		[Tooltip("How much blood reagent can it store?")]
		public float bloodReagentStoredMax = 0.5f;

		/// <summary>
		/// The amount of reagent at which the body part does not have enough to perform vital functions and starts
		/// taking damage
		/// </summary>
		[SerializeField]
		[Tooltip("At what reagent level does this start taking damage for not having enough?")]
		private float BloodDamageLow = 0;

		//private float bloodReagentStored = 0;

		/// <summary>
		/// The amount of of nutriment to consume in order to perform work, ie heal damage or replenish blood supply
		/// </summary>
		[Tooltip("How much nutriment does this consume to perform work?")]
		public float NutrimentConsumption = 0.02f;

		/// <summary>
		/// The amount of of nutriment to consumed each tick as part of passive metabolism
		/// </summary>
		[Tooltip("How much nutriment does this passively consume to each tick?")]
		public float NutrimentPassiveConsumption = 0.001f;

		/// <summary>
		/// The container in which the blood is stored
		/// </summary>
		[Tooltip("The part's associated blood pool")]
		public ReagentContainerBody BloodContainer = null;

		/// <summary>
		/// The nutriment reagent that this part consumes in order to perform tasks
		/// </summary>
		[SerializeField]
		[Tooltip("What does this live off?")]
		public Reagent Nutriment;

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

		/// <summary>
		/// Initializes the body part as part of the circulatory system
		/// </summary>
		public void BloodInitialise()
		{
			BloodContainer = this.GetComponent<ReagentContainerBody>();
			BloodContainer.Add(new ReagentMix(requiredReagent, bloodReagentStoredMax));
			//bloodReagentStored = bloodReagentStoredMax; //Organs spawn in oxygenated.
			BloodDamageLow = bloodReagentStoredMax * 0.25f;
			BloodContainer.ContentsSet = true;
		}

		#region BloodReagents
		/// ---------------------------
		/// Blood Reagent and Nutriment Methods
		/// ---------------------------
		/// Blood is implemented as a single container that holds two pools of blood, 'Used Blood' and 
		/// 'Ready Blood'. Ready Blood is the blood available to body parts to use, body parts take from
		/// this, remove reagents, and pass it into the Used Blood pool. Used Blood is then processed by
		/// other organs to turn it back into Ready Blood.

		/// There are two basic reagents that a body part can require to function. The first is 'Blood 
		/// Reagents', a common example of this is oxygen. These are essential to the function of the part,
		/// and the part will take damage if there is not enough of it in the Ready Blood pool, commonly
		/// because the lungs are failing. The second is 'Nutriments', these are required for the part to do
		/// work, such as healing, replenishing blood supply, and moving.  When there is not enough Nutriment
		/// in the blood the body part's efficieny will go down, causing sluggish movement and eventually
		/// unconsciousness and death as the heart and lungs fail.

		/// <summary>
		/// Handles the body part's use of blood for each tick. Consumes reagent to stay functional and nutriments
		/// as needed.
		/// </summary>
		public virtual void BloodUpdate()
		{
			//Can do something about purity
			//low Blood content punishment but no damage

			//Logger.Log("Available blood"  + BloodContainer[requiredReagent]);

			if (isBloodReagentConsumed)
			{
				if (BloodContainer[requiredReagent] < BloodDamageLow)
				{
					AffectDamage(1f, (int)DamageType.Oxy);
				}
				else
				{
					AffectDamage(-1f, (int)DamageType.Oxy);
				}

			}

			ReagentMix reagentMix = BloodContainer.TakeReagents(bloodReagentProcessed);
			if (isBloodReagentConsumed)
			{
				reagentMix.Remove(requiredReagent, Single.MaxValue);
			}

			HealthMaster.CirculatorySystem.UsedBloodPool.Add(reagentMix);

			if (TotalDamageWithoutOxy > 0 && BloodContainer[Nutriment] > 0)
			{

				float toConsume = NutrimentConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment, toConsume);
				NutrimentHeal(toConsume);
			}

			if (BloodContainer[Nutriment] > 0)
			{
				float toConsume = NutrimentPassiveConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment, toConsume);
				if (BloodContainer[Nutriment] < NutrimentPassiveConsumption * 1000)
				{
					//is Hungary
				}
			}
			else
			{
				//Is starving
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
		/// Can happen multiple times if there's multiple hearts. Supplys the body part with blood
		/// up to its capacity.
		/// </summary>
		/// <param name="bloodReagent"></param>
		/// <returns></returns>
		public ReagentMix BloodPumpedEvent(ReagentMix bloodReagent)
		{
			//Logger.Log("BloodPumpedEvent  " + bloodReagent);
			//Maybe have a dynamic 50% other blood in this blood
			// if (bloodReagent != requiredReagent)
			// {
			// return HandleWrongBloodReagent(bloodReagent, amountOfBloodReagentPumped);
			// }
			//bloodReagent.Subtract()
			//BloodContainer.Add(bloodReagent);
			if ((BloodContainer.ReagentMixTotal + bloodReagent.Total) > BloodContainer.MaxCapacity)
			{
				float BloodToTake = (BloodContainer.MaxCapacity - BloodContainer.ReagentMixTotal);
				bloodReagent.TransferTo(BloodContainer.CurrentReagentMix, BloodToTake);
				BloodContainer.OnReagentMixChanged?.Invoke();
				BloodContainer.ReagentsChanged();
				return bloodReagent;
			}
			else
			{
				bloodReagent.TransferTo(BloodContainer.CurrentReagentMix, bloodReagent.Total);
				BloodContainer.OnReagentMixChanged?.Invoke();
				BloodContainer.ReagentsChanged();
			}

			return bloodReagent;
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