using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	[RequireComponent(typeof(ItemAttributesV2))]
	public class ImplantBase : MonoBehaviour
	{
		[SerializeField]
		private List<ImplantProperty> properties = new List<ImplantProperty>();

		private ItemAttributesV2 attributes;

		private float health = 100;

		[SerializeField]
		[Tooltip("The maxmimum health of the implant." +
		         "Implants will start with this amount of health.")]
		private float maxHealth = 100;

		//This should be utilized in most implants so as to make changing the effectivenss of it easy.
		//Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
		[SerializeField]
		[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
		         "Can be modified by implant modifiers.")]
		private float efficiency = 1;

		[SerializeField]
		[Tooltip("Do we consume any reagent in our blood?")]
		private bool isBloodReagentConsumed = false;

		[SerializeField] [Tooltip("What reagent do we use?")]
		private Chemistry.Reagent requiredReagent;

		[SerializeField]
		[Tooltip("How much blood reagent do we actually consume per second?")]
		private float bloodReagentConsumed = 0.15f;

		[SerializeField]
		[Tooltip("How much blood reagent is stored per blood pump event.")]
		private float bloodReagentStoreAmount = 0.01f;

		[SerializeField]
		[Tooltip("Can we store any blood reagent?")]
		private float bloodReagentStoredMax = 20f;

		private float bloodReagentStored = 0;

		[SerializeField]
		private BodyPartType bodyPartType;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents." +
		         "It is intended for things like arms/legs/heads." +
		         "Leave empty if it shouldn't change this.")]
		private SpriteDataSO limbSpriteData;
		public SpriteDataSO LimbSpriteData => limbSpriteData;

		[SerializeField]
		[Tooltip("The overlaying visuals of this implant. It will be laid on top of the limb sprite." +
		         "Leave empty if there should be no visual representation.")]
		private SpriteDataSO limbOverlaySpriteData;
		public SpriteDataSO LimbOverlaySpriteData => limbOverlaySpriteData;

		[SerializeField]
		private bool implantPreventsBleeding = true;

		private void Awake()
		{
			attributes = GetComponent<ItemAttributesV2>();
			bloodReagentStored = bloodReagentStoredMax; //Organs spawn in oxygenated.
			health = maxHealth;
		}

		public virtual void ImplantUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (ImplantProperty prop in properties)
			{
				prop.ImplantUpdate(this, healthMaster);
			}

			bloodReagentStored -= Time.deltaTime * bloodReagentConsumed;
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

		/// <summary>
		/// This is called whenever blood is pumped through the circulatory system by a heartbeat.
		/// Can happen multiple times if there's multiple hearts.
		/// </summary>
		/// <param name="bloodReagent"></param>
		/// <param name="amountOfBloodReagentPumped"></param>
		/// <returns></returns>
		public float BloodPumpedEvent(Chemistry.Reagent bloodReagent, float amountOfBloodReagentPumped)
		{
			if (bloodReagent != requiredReagent)
			{
				return HandleWrongBloodReagent(bloodReagent, amountOfBloodReagentPumped);
			}

			float returnedReagent = amountOfBloodReagentPumped;

			if (isBloodReagentConsumed)
			{
				//No reagent left in blood, we sad.
				if (returnedReagent < 0f)
				{
					return 0f;
				}

				returnedReagent -= bloodReagentStoreAmount;
				bloodReagentStored += bloodReagentStoreAmount;

			}

			return returnedReagent;
		}
	}

}
