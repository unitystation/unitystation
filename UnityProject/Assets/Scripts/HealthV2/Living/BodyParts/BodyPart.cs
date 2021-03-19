using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Systems.Clothing;
using Items;

namespace HealthV2
{
	public partial class BodyPart : MonoBehaviour, IBodyPartDropDownOrgans
	{
		[SerializeField]
		// [Required("Need a health master to send updates too." +
		// "Will attempt to find a components in its parents if not already set in editor.")]
		protected LivingHealthMasterBase HealthMaster = null;


		public LivingHealthMasterBase healthMaster
		{
			get { return HealthMaster; }
			set
			{
				HealthMaster = value;
				for (int i = containBodyParts.Count; i >= 0; i--)
				{
					if (i < containBodyParts.Count)
					{
						SetUpBodyPart(containBodyParts[i]);
					}

				}

				HealthMasterSet();
			}
		}

		public List<BodyPartSprites> RelatedPresentSprites = new List<BodyPartSprites>();

		public ItemStorage storage = null;


		[SerializeField] public BodyPartType bodyPartType;

		//The implanted things in this
		[SerializeField] private List<BodyPart> containBodyParts = new List<BodyPart>();

		public  List<BodyPart>  ContainBodyParts => containBodyParts;

		private ItemAttributesV2 attributes;


		//This should be utilized in most implants so as to make changing the effectivenss of it easy.
		//Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
		[SerializeField]
		[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
		         "Can be modified by implant modifiers.")]
		private float efficiency = 1;


		[SerializeField] [Tooltip("Does the sprite change dependning on Gender?")]
		private bool isDimorphic = false;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents." +
		         "It is intended for things like arms/legs/heads." +
		         "Leave empty if it shouldn't change this.")]
		private BodyTypesWithOrder BodyTypesSprites = new BodyTypesWithOrder();


		private ListSpriteDataSOWithOrder limbSpriteData;

		//Needs to be converted over four sexes/Body type
		public ListSpriteDataSOWithOrder LimbSpriteData => limbSpriteData;


		public RootBodyPartContainer Root;

		public BodyPart ContainedIn;


		public BodyPartSprites SpritePrefab;

		public bool BodySpriteSet = false;

		public BodyPartCustomisationBase LobbyCustomisation;

		public List<BodyPart> OptionalOrgans => optionalOrgans;

		[Tooltip("The organs that can be put inside of this")]
		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();

		[Tooltip("The organ that this can be replaced with")]
		public List<BodyPart> OptionalReplacementOrgan = new List<BodyPart>();

		public bool isSurface = false;

		public ClothingHideFlags ClothingHide;


		public virtual void HealthMasterSet()
		{
			if (BodySpriteSet == false)
			{
				//If gendered part then set the sprite limb data to it
				if (isDimorphic)
				{
					limbSpriteData = new ListSpriteDataSOWithOrder();
					limbSpriteData.SpriteOrder = BodyTypesSprites.SpriteOrder;
					limbSpriteData.Sprites = BodyTypesSprites.BodyTypes[(int) healthMaster.BodyType].Sprites;
				}
				else
				{
					limbSpriteData = new ListSpriteDataSOWithOrder();
					limbSpriteData.SpriteOrder = BodyTypesSprites.SpriteOrder;
					if (BodyTypesSprites.BodyTypes.Count > 0)
					{
						limbSpriteData.Sprites = BodyTypesSprites.BodyTypes[(int) BodyType.NonBinary].Sprites;
					}
				}

				BodySpriteSet = true;
			}
			UpdateIcons();
		}

		public Tuple<SpriteOrder, List<SpriteDataSO>> GetBodyTypeSprites(BodyType BodyType)
		{
			if (BodyTypesSprites.BodyTypes.Count > (int) BodyType)
			{

				return new Tuple<SpriteOrder, List<SpriteDataSO>>(BodyTypesSprites.SpriteOrder,
					BodyTypesSprites.BodyTypes[(int) BodyType].Sprites);
			}
			else
			{
				if (BodyTypesSprites.BodyTypes.Count > 0)
				{
					return new Tuple<SpriteOrder, List<SpriteDataSO>>(BodyTypesSprites.SpriteOrder,
						BodyTypesSprites.BodyTypes[0].Sprites);
				}
			}

			return new Tuple<SpriteOrder, List<SpriteDataSO>>(new SpriteOrder(), new List<SpriteDataSO>());
		}

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
			DamageInitialisation();


			health = maxHealth;

			UpdateSeverity();
			Initialisation();

		}


		public virtual void Initialisation()
		{
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
			//TODOH backwards for i
			foreach (BodyPart prop in containBodyParts)
			{
				prop.ImplantPeriodicUpdate(healthMaster);
			}

			BloodUpdate();
			CalculateRadiationDamage();
		}


		public virtual void AddBodyPart(GameObject IngameObject)
		{
			storage.ServerTryAdd(IngameObject);
		}

		public virtual void AddBodyPartSlot(ItemSlot ItemSlot)
		{
			storage.ServerTryTransferFrom(ItemSlot);
		}

		public virtual void RemoveFromBodyThis()
		{
			var parent = this.GetParent();
			if (parent != null)
			{
				parent.RemoveSpecifiedFromThis(this.gameObject);
			}
		}

		public virtual void RemoveSpecifiedFromThis(GameObject inOrgan)
		{
			storage.ServerTryRemove(inOrgan);
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
			if (healthMaster == null) return;
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
				containBodyParts.Remove(implant);
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

	[System.Serializable]
	public class BodyTypesWithOrder
	{
		public SpriteOrder SpriteOrder;

		[Tooltip("NonBinary, male, female, Other1, other2 , ect.")]
		public List<ListSpriteDataSO> BodyTypes = new List<ListSpriteDataSO>();
	}

	[System.Serializable]
	public class ListSpriteDataSO
	{
		public List<SpriteDataSO> Sprites = new List<SpriteDataSO>();
	}


	[System.Serializable]
	public class ListSpriteDataSOWithOrder
	{
		public SpriteOrder SpriteOrder;
		public List<SpriteDataSO> Sprites = new List<SpriteDataSO>();
	}
}