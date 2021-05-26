using System.Collections.Generic;
using Items;
using UnityEngine;
using NaughtyAttributes;

namespace HealthV2
{
	[RequireComponent(typeof(ItemAttributesV2))]
	public class ImplantBase : MonoBehaviour
	{
		//The implanted things in this
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

		[SerializeField]
		[Tooltip("Does the sprite change dependning on Gender?")]
		private bool isDimorphic = false;

		[SerializeField]
		[ShowIf(nameof(isDimorphic))]
		[Tooltip("Bodypart Gender")]
		private Gender gender = Gender.Male;

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
		private float BloodStoredMax = 20f;

		private float bloodReagentStored = 0;

		[SerializeField]
		private BodyPartType bodyPartType;

		[SerializeField]
		[ShowIf(nameof(isDimorphic))]
		[Tooltip("The MALE visuals of this implant")]
		private List<SpriteDataSO>  maleSprite;


		[SerializeField]
		[ShowIf(nameof(isDimorphic))]
		[Tooltip("The FEMALE visuals of this implant")]
		private List<SpriteDataSO>  femaleSprite;

		[SerializeField]
		[ShowIf(nameof(isDimorphic))]
		[Tooltip("The NONBINARY/NEUTRAL visuals of this implant (is also used as fallback sprite)")]
		private List<SpriteDataSO>  nonbinarySprite;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents." +
		         "It is intended for things like arms/legs/heads." +
		         "Leave empty if it shouldn't change this.")]
		private List<SpriteDataSO>  limbSpriteData;
		public List<SpriteDataSO>  LimbSpriteData => limbSpriteData;

		private void Awake()
		{
			attributes = GetComponent<ItemAttributesV2>();
			bloodReagentStored = BloodStoredMax; //Organs spawn in oxygenated.
			health = maxHealth;
			//If gendered part then set the sprite limb data to it
			if (isDimorphic)
			{
				switch (gender)
				{
					case Gender.Male:
						limbSpriteData = maleSprite;
						break;
					case Gender.Female:
						limbSpriteData = femaleSprite;
						break;
					case Gender.NonBinary:
						limbSpriteData = nonbinarySprite;
						break;
					default:
						//TODO: error log 
						limbSpriteData = nonbinarySprite; //set NB as fallbackk
						break;
				}
			}

			Initialise();

		}

		public virtual void Initialise()
		{

		}


		public virtual void AddedToBody(LivingHealthMasterBase livingHealthMasterBase)
		{

		}

		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{

		}


		public virtual void ImplantUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (ImplantProperty prop in properties)
			{
				//prop.ImplantUpdate(this, healthMaster);
			}

			bloodReagentStored -= Time.deltaTime * bloodReagentConsumed;
		}

	}

}
