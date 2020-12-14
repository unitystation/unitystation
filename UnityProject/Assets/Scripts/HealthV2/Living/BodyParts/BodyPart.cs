using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

namespace HealthV2
{
	public partial class BodyPart : MonoBehaviour
	{

		[SerializeField]
		[Required("Need a health master to send updates too." +
		          "Will attempt to find a components in its parents if not already set in editor.")]
		private LivingHealthMasterBase HealthMaster = null;


		public LivingHealthMasterBase healthMaster
		{
			get
			{
				return HealthMaster;
			}
			set
			{
				HealthMaster = value;
				foreach (var BodyPart in containBodyParts)
				{
					SetUpBodyPart(BodyPart);
				}
			}
		}

		public List<BodyPartSprites> RelatedPresentSprites = new List<BodyPartSprites>();

		public ItemStorage storage = null;


		[SerializeField] public BodyPartType bodyPartType;

		//The implanted things in this
		[SerializeField] private List<BodyPart> containBodyParts = new List<BodyPart>();

		private ItemAttributesV2 attributes;

		private float health = 100;

		[SerializeField]
		[Tooltip("The maxmimum health of the implant." +
		         "Implants will start with this amount of health.")]
		private float maxHealth = 100; //Is used for organ functionIt

		//This should be utilized in most implants so as to make changing the effectivenss of it easy.
		//Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
		[SerializeField]
		[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
		         "Can be modified by implant modifiers.")]
		private float efficiency = 1;



		[SerializeField] [Tooltip("Does the sprite change dependning on Gender?")]
		private bool isDimorphic = false;

		[SerializeField] [ShowIf(nameof(isDimorphic))] [Tooltip("Bodypart Gender")]
		private Gender gender = Gender.Male;

		[SerializeField] [ShowIf(nameof(isDimorphic))] [Tooltip("The MALE visuals of this implant")]
		private List<SpriteDataSO> maleSprite;


		[SerializeField] [ShowIf(nameof(isDimorphic))] [Tooltip("The FEMALE visuals of this implant")]
		private List<SpriteDataSO> femaleSprite;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents." +
		         "It is intended for things like arms/legs/heads." +
		         "Leave empty if it shouldn't change this.")]
		private List<SpriteDataSO> limbSpriteData;

		public List<SpriteDataSO> LimbSpriteData => limbSpriteData;


		public RootBodyPartContainer Root;

		public BodyPart ContainedIn;


		public BodyPartSprites SpritePrefab;

		void Awake()
		{
			foreach (BodyPartSprites b in GetComponentsInParent<BodyPartSprites>())
			{
				if (b.bodyPartType.Equals(bodyPartType))
				{
					Debug.Log(b);
				}

				//TODO: Do we need to add listeners for implant removal
			}

			storage = GetComponent<ItemStorage>();
			storage.ServerInventoryItemSlotSet += ImplantAdded;

			attributes = GetComponent<ItemAttributesV2>();
			BloodInitialise();
			health = maxHealth;
			//If gendered part then set the sprite limb data to it
			if (isDimorphic)
			{
				if (gender == Gender.Male)
				{
					limbSpriteData = maleSprite;
				}
				else if (gender == Gender.Female)
				{
					limbSpriteData = femaleSprite;
				}
				else
				{
					//TODO: Error log
				}
			}
		}


		public virtual void ImplantUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (BodyPart prop in containBodyParts)
			{
				prop.ImplantUpdate(healthMaster);
			}
		}

		public virtual void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
		{
			foreach (BodyPart prop in containBodyParts)
			{
				prop.ImplantPeriodicUpdate(healthMaster);
			}
			BloodUpdate();
		}


		public virtual void AddedToBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			if (ContainedIn != null)
			{
				ContainedIn.SubBodyPartAdded(this);
			}
		}

		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			if (ContainedIn != null)
			{
				ContainedIn.SubBodyPartRemoved(this);
			}
		}

		public dynamic GetParent()
		{
			if (ContainedIn != null)
			{
				return ContainedIn;
			}
			else if (Root != null)
			{
				return Root;
			}
			else
			{
				return null;
				//Is not in any*body* :P
			}
		}

		public virtual void SubBodyPartRemoved(BodyPart implant)
		{
			var Parent = GetParent();
			if (Parent != null)
			{
				Parent.SubBodyPartRemoved(implant);
			}
		}

		public virtual void SubBodyPartAdded(BodyPart implant)
		{
			var Parent = GetParent();
			if (Parent != null)
			{
				Parent.SubBodyPartAdded(implant);
			}
		}


		public void SetUpBodyPart(BodyPart implant)
		{
			implant.healthMaster = healthMaster;
			healthMaster.AddNewImplant(implant);
			implant.AddedToBody(healthMaster);
		}



		public virtual void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			//Check what's being added and add sprites if appropriate
			if (newImplant)
			{

				BodyPart implant = newImplant.GetComponent<BodyPart>();
				containBodyParts.Add(implant);
				implant.ContainedIn = this;
				//Initialisation jizz
				if (healthMaster != null)
				{
					SetUpBodyPart(implant);
				}
			}

			//Remove sprites if appropriate
			if (prevImplant)
			{
				BodyPart implant = prevImplant.GetComponent<BodyPart>();
				implant.healthMaster = null;
				healthMaster.RemoveImplant(implant);
				implant.RemovedFromBody(healthMaster);
				implant.ContainedIn = null;
				//bodyPartSprites?.UpdateSpritesOnImplantRemoved(implant);
			}
		}

		public List<BodyPart> GetAllBodyPartsAndItself(List<BodyPart> ReturnList)
		{
			ReturnList.Add(this);
			foreach (var BodyPart in containBodyParts)
			{
				BodyPart.GetAllBodyPartsAndItself(ReturnList);
			}

			return ReturnList;
		}

	}
}