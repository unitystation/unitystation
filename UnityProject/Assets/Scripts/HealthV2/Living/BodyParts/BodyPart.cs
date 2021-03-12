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

		/// <summary>
		/// A storage container for all of the item versions of the things contained within the body part,
		/// usually other body parts.
		/// </summary>
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

		#region BodyPartStorage

		/// ---------------------------
		/// Body Part Storage Methods
		/// ---------------------------
		/// Body parts are capable of storing other body parts, and are themselves stored in either a body part
		/// or a Body Part Container.  Additionally, adding or removing a body part from 'storage' is a two part
		/// process: the storage of the actual body part item, and the connecting of the body part to the health
		/// system.  Think of it like an electronic device: putting it into a storage is like placing it in a 
		/// room, adding it to a body is like plugging it in.  If you just put it into a room it wont work until
		/// you plug it in, and you shouldn't try to plug something in until you've moved it into the room first.

		/// To complicate things, a body part doesn't know what 'depth' it is, and only can talk to its parent
		/// (the body part that contains it), and each body part needs to know all of the body parts it does and 
		/// doesn't contain in order to coordinate.  We accomplish this by each organ telling its parent when its 
		/// contents change, and the parent tell the parent's parent and so on until it reaches the Health Master.

		/// <summary>
		/// Adds an object to the body part's internal storage, usually another body part
		/// </summary>
		/// <param name="IngameObject">Object to try and store in the Body Part</param>
		public virtual void AddBodyPart(GameObject IngameObject)
		{
			storage.ServerTryAdd(IngameObject);
		}

		/// <summary>
		/// Transfers an item from an item slot to the body part's internal storage, usually another body part
		/// </summary>
		/// <param name="ItemSlot">Item Slot to transfer from</param>
		public virtual void AddBodyPartSlot(ItemSlot ItemSlot)
		{
			storage.ServerTryTransferFrom(ItemSlot);
		}

		/// <summary>
		/// Removes the Body Part Item from the storage of its parent (a body part container or another body part)
		/// </summary>
		public virtual void RemoveFromBodyThis()
		{
			var parent = this.GetParent();
			if (parent != null)
			{
				parent.RemoveSpecifiedFromThis(this.gameObject);
			}
		}

		/// <summary>
		/// Removes a specified item from the body part's storage
		/// </summary>
		/// <param name="inOrgan">Item to remove</param>
		public virtual void RemoveSpecifiedFromThis(GameObject inOrgan)
		{
			storage.ServerTryRemove(inOrgan);
		}

		/// <summary>
		/// Adds this body part to a host body system
		/// </summary>
		/// <param name="livingHealthMasterBase">Body to be added to</param>
		public virtual void AddedToBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			if (ContainedIn != null)
			{
				ContainedIn.SubBodyPartAdded(this);
			}
		}

		/// <summary>
		/// Removes this body part from its host body system
		/// </summary>
		/// <param name="livingHealthMasterBase">Body to be removed from</param>
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			if (ContainedIn != null)
			{
				ContainedIn.SubBodyPartRemoved(this);
			}
		}

		/// <summary>
		/// Gets the part of the body that the body part resides in (a body part container or another body part)
		/// </summary>
		/// <returns>A body part that contains it OR a body part container that contains it OR null if it is not
		/// contained in anything</returns>
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

		/// <summary>
		/// Tells this body part's parent to remove a specified body part contained within this body part from the
		/// host body system.
		/// </summary>
		/// <param name="implant">Body Part to be removed</param>
		public virtual void SubBodyPartRemoved(BodyPart implant)
		{
			var Parent = GetParent();
			if (Parent != null)
			{
				Parent.SubBodyPartRemoved(implant);
			}
		}

		/// <summary>
		/// Adds a body part contained within this body part to the host body system
		/// </summary>
		/// <param name="implant">Body Part to be added</param>
		public virtual void SubBodyPartAdded(BodyPart implant)
		{
			var Parent = GetParent();
			if (Parent != null)
			{
				Parent.SubBodyPartAdded(implant);
			}
		}

		/// <summary>
		/// Initializes a body part contained within this body part, setting it to use the same
		/// health master as this body part.
		/// </summary>
		/// <param name="implant">Body Part to be initialized</param>
		public void SetUpBodyPart(BodyPart implant)
		{
			implant.HealthMaster = HealthMaster;
			if (HealthMaster == null) return;
			HealthMaster.AddNewImplant(implant);
			implant.AddedToBody(HealthMaster);
		}

		/// <summary>
		/// Adds a new body part to this body part, and removes the old part whose place is
		/// being taken if possible
		/// </summary>
		/// <param name="implant">Old body part to be removed</param>
		/// <param name="implant">New body part to be added</param>
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

		/// <summary>
		/// Takes a list and adds this body part to it, all body parts contained within this body part, as well
		/// as all body parts contained within those body parts, etc.
		/// </summary>
		/// <param name="ReturnList">List to be added to</param>
		/// <returns>The list with added body parts</returns>
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

	#endregion

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