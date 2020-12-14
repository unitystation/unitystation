using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public partial class BodyPart
	{
		[SerializeField] [Tooltip("Do we consume any reagent in our blood?")]
		private bool isBloodReagentConsumed = false;

		public bool IsBloodReagentConsumed => isBloodReagentConsumed;

		[SerializeField] [Tooltip("What reagent do we use?")]
		private Chemistry.Reagent requiredReagent;

		[SerializeField] [Tooltip("How much blood reagent do we actually consume per second?")]
		private float bloodReagentConsumed = 0.15f;

		[SerializeField] [Tooltip("How much blood reagent is stored per blood pump event.")]
		private float bloodReagentStoreAmount = 0.01f;

		public float BloodReagentStoreAmount => bloodReagentStoreAmount;

		[Tooltip("Can we store any blood reagent?")]
		private float bloodReagentStoredMax = 0.5f;

		private float BloodDamageLow = 0;

		private float bloodReagentStored = 0;


		public void BloodInitialise()
		{
			bloodReagentStored = bloodReagentStoredMax; //Organs spawn in oxygenated.
			BloodDamageLow = bloodReagentStoredMax * 0.25f;
		}

		public void BloodUpdate()
		{
			if (bloodReagentStored < BloodDamageLow)
			{
				AffectDamage( 1f, DamageType.Oxy );
			}
			else
			{
				AffectDamage( -1f,  DamageType.Oxy);
			}

			float BloodUsed = bloodReagentConsumed;
			if (bloodReagentStored < BloodUsed)
			{
				BloodUsed = bloodReagentStored;
				bloodReagentStored = 0;
			}
			else
			{
				bloodReagentStored -= BloodUsed;
			}

			healthMaster.CirculatorySystem.UseBloodPool += BloodUsed;
		}

		/// <summary>
		/// This is called whenever blood is pumped through the circulatory system by a heartbeat.
		/// Can happen multiple times if there's multiple hearts.
		/// </summary>
		/// <param name="bloodReagent"></param>
		/// <param name="amountOfBloodReagentPumped"></param>
		/// <returns></returns>
		public float BloodPumpedEvent(Chemistry.Reagent bloodReagent, float amountOfBloodReagentPumped)
		{
			//Maybe have a dynamic 50% other blood in this blood
			if (bloodReagent != requiredReagent)
			{
				return HandleWrongBloodReagent(bloodReagent, amountOfBloodReagentPumped);
			}


			bloodReagentStored += amountOfBloodReagentPumped;
			if (bloodReagentStored > bloodReagentStoredMax)
			{
				float BloodReturn = bloodReagentStored - bloodReagentStoredMax;
				bloodReagentStored = bloodReagentStoredMax;
				return BloodReturn;
			}
			return 0;
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