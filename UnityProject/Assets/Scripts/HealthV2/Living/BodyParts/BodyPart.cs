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
	/// <summary>
	/// A part of a body. Can be external, such as a limb, or internal ie an organ
	/// Body parts can also contain other body parts, ie brain body part in the head body part
	/// </summary>
	public partial class BodyPart : MonoBehaviour, IBodyPartDropDownOrgans
	{
		[SerializeField]
		// [Required("Need a health master to send updates too." +
		// "Will attempt to find a components in its parents if not already set in editor.")]
		protected LivingHealthMasterBase healthMaster = null;

		public LivingHealthMasterBase HealthMaster
		{
			get { return healthMaster; }
			set
			{
				healthMaster = value;
				for (int i = ContainBodyParts.Count; i >= 0; i--)
				{
					if (i < ContainBodyParts.Count)
					{
						SetUpBodyPart(ContainBodyParts[i]);
					}
				}
				HealthMasterSet();
			}
		}

		public ItemStorage storage = null;

		/// <summary>
		/// The category that this body part falls under for purposes of targeting with the UI
		/// </summary>
		[SerializeField] public BodyPartType bodyPartType;

		/// <summary>
		/// The list of body parts contained within this body part
		/// </summary>
		[SerializeField] public List<BodyPart> ContainBodyParts { get; private set; } = new List<BodyPart>();

		private ItemAttributesV2 attributes;

		//This should be utilized in most implants so as to make changing the effectivenss of it easy.
		//Some organs wont boil down to just one efficiency score, so you'll have to keep that in mind.
		[SerializeField]
		[Tooltip("This is a generic variable representing the 'efficieny' of the implant." +
				 "Can be modified by implant modifiers.")]
		private float efficiency = 1;


		[SerializeField]
		[Tooltip("Does the sprite change depending on Gender?")]
		private bool isDimorphic = false;

		/// <summary>
		/// The body part 'container' to which this body part belongs, (ie legs group, arms group), if any
		/// </summary>
		public RootBodyPartContainer Root;

		/// <summary>
		/// The body part in which this body part is contained, if any
		/// </summary>
		public BodyPart ContainedIn;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents." +
				 "It is intended for things like arms/legs/heads." +
				 "Leave empty if it shouldn't change this.")]
		private BodyTypesWithOrder BodyTypesSprites = new BodyTypesWithOrder();

		/// <summary>
		/// The list of sprites associated with this body part
		/// </summary>
		public List<BodyPartSprites> RelatedPresentSprites = new List<BodyPartSprites>();

		//Needs to be converted over four sexes/Body type
		public ListSpriteDataSOWithOrder LimbSpriteData { get; private set; }

		/// <summary>
		/// The prefab sprites for this body part
		/// </summary>
		public BodyPartSprites SpritePrefab;

		/// <summary>
		/// Boolean for whether the sprites for the body part have been set, returns true when they are
		/// </summary>
		public bool BodySpriteSet = false;

		/// <summary>
		/// Custom settings from the lobby character designer, unimplemented
		/// </summary>
		public BodyPartCustomisationBase LobbyCustomisation;

		[Tooltip("The organs that can be put inside of this")]
		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();
		public List<BodyPart> OptionalOrgans => optionalOrgans;

		[Tooltip("The organ that this can be replaced with")]
		public List<BodyPart> OptionalReplacementOrgan = new List<BodyPart>();

		/// <summary>
		/// Boolean that is true if the body part is external (exposed to the outside world), false if it is internal
		/// </summary>
		public bool isSurface = false;

		public ClothingHideFlags ClothingHide;

		/// <summary>
		/// Initializes the body part
		/// </summary>
		public virtual void HealthMasterSet()
		{
			if (BodySpriteSet == false)
			{
				//If gendered part then set the sprite limb data to it
				if (isDimorphic)
				{
					LimbSpriteData = new ListSpriteDataSOWithOrder();
					LimbSpriteData.SpriteOrder = BodyTypesSprites.SpriteOrder;
					LimbSpriteData.Sprites = BodyTypesSprites.BodyTypes[(int)HealthMaster.BodyType].Sprites;
				}
				else
				{
					LimbSpriteData = new ListSpriteDataSOWithOrder();
					LimbSpriteData.SpriteOrder = BodyTypesSprites.SpriteOrder;
					if (BodyTypesSprites.BodyTypes.Count > 0)
					{
						LimbSpriteData.Sprites = BodyTypesSprites.BodyTypes[(int)BodyType.NonBinary].Sprites;
					}
				}

				BodySpriteSet = true;
			}
		}

		/// <summary>
		/// Gets the sprites for the body part based of a specified body type
		/// </summary>
		/// <param name="BodyType">The body type to get sprites for</param>
		/// <returns>List of SpriteDataSO's</returns>
		public Tuple<SpriteOrder, List<SpriteDataSO>> GetBodyTypeSprites(BodyType BodyType)
		{
			if (BodyTypesSprites.BodyTypes.Count > (int)BodyType)
			{

				return new Tuple<SpriteOrder, List<SpriteDataSO>>(BodyTypesSprites.SpriteOrder,
					BodyTypesSprites.BodyTypes[(int)BodyType].Sprites);
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

		public virtual void ImplantUpdate()
		{
			foreach (BodyPart prop in ContainBodyParts)
			{
				prop.ImplantUpdate();
			}
		}

		/// <summary>
		/// Updates the body part according to the related systems (default: blood system, radiation damage)
		/// </summary>
		public virtual void ImplantPeriodicUpdate()
		{
			//TODOH backwards for i
			foreach (BodyPart prop in ContainBodyParts)
			{
				prop.ImplantPeriodicUpdate();
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
			implant.HealthMaster = HealthMaster;
			if (HealthMaster == null) return;
			HealthMaster.AddNewImplant(implant);
			implant.AddedToBody(HealthMaster);
		}

		public virtual void ImplantAdded(Pickupable prevImplant, Pickupable newImplant)
		{
			//Check what's being added and add sprites if appropriate
			if (newImplant)
			{
				BodyPart implant = newImplant.GetComponent<BodyPart>();
				ContainBodyParts.Add(implant);
				implant.ContainedIn = this;
				//Initialisation jizz
				if (HealthMaster != null)
				{
					SetUpBodyPart(implant);
				}
			}

			//Remove sprites if appropriate
			if (prevImplant)
			{
				BodyPart implant = prevImplant.GetComponent<BodyPart>();
				implant.HealthMaster = null;
				HealthMaster.RemoveImplant(implant);
				implant.RemovedFromBody(HealthMaster);
				implant.ContainedIn = null;
				ContainBodyParts.Remove(implant);
				//bodyPartSprites?.UpdateSpritesOnImplantRemoved(implant);
			}
		}

		public List<BodyPart> GetAllBodyPartsAndItself(List<BodyPart> ReturnList)
		{
			ReturnList.Add(this);
			foreach (var BodyPart in ContainBodyParts)
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