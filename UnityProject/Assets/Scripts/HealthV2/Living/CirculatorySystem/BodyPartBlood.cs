using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using Chemistry.Components;
using NaughtyAttributes;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		[SerializeField] [Tooltip("Do we consume any reagent in our blood?")]
		private bool isBloodReagentConsumed = false;

		public bool IsBloodReagentConsumed => isBloodReagentConsumed;

		[SerializeField] [Tooltip("Does this have any blood Flow at all?")]
		private bool isBloodReagentCirculated = true;

		public bool IsBloodReagentCirculated => isBloodReagentCirculated;


		[SerializeField] [Tooltip("What reagent do we use?")]
		protected Chemistry.Reagent requiredReagent;

		[SerializeField] [Tooltip("How much blood reagent do we actually consume per second?")]
		private float _bloodReagentConsumed = 0.10f;

		[SerializeField] [Tooltip("How much blood it processes per second")]
		private float bloodReagentProcessed = 0.15f;

		[SerializeField] [Tooltip("How much blood reagent is stored per blood pump event.")]
		private float bloodReagentStoreAmount = 0.01f;

		public float BloodReagentStoreAmount => bloodReagentStoreAmount;

		[Tooltip("Can we store any blood reagent?")]
		public  float bloodReagentStoredMax = 0.5f;

		private float BloodDamageLow = 0;

		//private float bloodReagentStored = 0;

		public float NutrimentConsumption = 0.02f;
		public float NutrimentPassiveConsumption = 0.001f;


		public ReagentContainerBody BloodContainer = null;

		[SerializeField] [Tooltip("What does this live off?")]
		public Reagent Nutriment;

		public event Action ModifierChange;

		public float TotalModified = 1;

		public List<Modifier> AppliedModifiers = new List<Modifier>();

		public void UpdateMultiplier()
		{
			TotalModified = 1;
			foreach (var Modifier in AppliedModifiers)
			{
				TotalModified *= Mathf.Max(0, Modifier.Multiplier);
			}
			ModifierChange?.Invoke();
		}


		public void AddModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = this;
			AppliedModifiers.Add(InModifier);
		}

		public void RemoveModifier(Modifier InModifier)
		{
			InModifier.RelatedPart = null;
			AppliedModifiers.Remove(InModifier);
		}


		public void BloodInitialise()
		{
			BloodContainer = this.GetComponent<ReagentContainerBody>();
			BloodContainer.Add(new ReagentMix(requiredReagent, bloodReagentStoredMax));
			//bloodReagentStored = bloodReagentStoredMax; //Organs spawn in oxygenated.
			BloodDamageLow = bloodReagentStoredMax * 0.25f;
			BloodContainer.ContentsSet = true;
		}

		public virtual void BloodUpdate()
		{
			//Can do something about purity
			//low Blood content punishment but no damage

			//Logger.Log("Available blood"  + BloodContainer[requiredReagent]);

			if (isBloodReagentConsumed)
			{
				if (BloodContainer[requiredReagent] < BloodDamageLow)
				{
					AffectDamage(1f, (int) DamageType.Oxy);
				}
				else
				{
					AffectDamage(-1f, (int) DamageType.Oxy);
				}

			}

			ReagentMix reagentMix = BloodContainer.TakeReagents(bloodReagentProcessed);
			if (isBloodReagentConsumed)
			{
				reagentMix.Remove(requiredReagent, Single.MaxValue);
			}

			healthMaster.CirculatorySystem.UseBloodPool.Add(reagentMix);

			if (TotalDamageWithoutOxy > 0 && BloodContainer[Nutriment]> 0)
			{

				float toConsume = NutrimentConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment,toConsume );
				NutrimentHeal(toConsume);
			}

			if (BloodContainer[Nutriment] > 0)
			{
				float toConsume = NutrimentPassiveConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment,toConsume );
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

		public void NutrimentHeal(double Amount)
		{
			double DamageMultiplier = TotalDamageWithoutOxy / Amount;

			for (int i = 0; i < Damages.Length; i++)
			{
				if ((int) DamageType.Oxy == i) continue;
				HealDamage(null, (float) (Damages[i] / DamageMultiplier), i);
			}
		}

		/// <summary>
		/// This is called whenever blood is pumped through the circulatory system by a heartbeat.
		/// Can happen multiple times if there's multiple hearts.
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
				float BloodToTake = (BloodContainer.MaxCapacity - BloodContainer.ReagentMixTotal );
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