using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Systems.Clothing;
using UI.CharacterCreator;

namespace HealthV2
{
	/// <summary>
	/// A part of a body. Can be external, such as a limb, or internal like an organ.
	/// Body parts can also contain other body parts, eg the 'brain body part' contained in the 'head body part'.
	/// BodyPart is a partial class split into BodyPart, BodyPartDamage, BodyPartBlood, BodyPartSurgery, and BodyPartModifiers.
	/// </summary>
	public partial class BodyPart : MonoBehaviour, IBodyPartDropDownOrgans
	{
		[SerializeField]
		[Tooltip("The Health Master associated with this part, will find from parents if not set in editor")]
		protected LivingHealthMasterBase healthMaster = null;

		public LivingHealthMasterBase HealthMaster
		{
			get { return healthMaster; }
			set
			{
				healthMaster = value;
				foreach (var bodyPart in ContainBodyParts)
				{
					SetUpBodyPart(bodyPart);
				}
				HealthMasterSet();
			}
		}

		/// <summary>
		/// Storage container for things (usually other body parts) held within this body part
		/// </summary>
		[Tooltip("Things (eg other organs) held within this")]
		public ItemStorage Storage = null;

		/// <summary>
		/// The category that this body part falls under for targeting purposes
		/// </summary>
		[Tooltip("The category that this body part falls under for targeting purposes")]
		[SerializeField] public BodyPartType BodyPartType;

		/// <summary>
		/// The list of body parts contained within this body part
		/// </summary>
		[Tooltip("List of body parts contained within this")]
		[SerializeField] private List<BodyPart> containBodyParts = new List<BodyPart>();
		public List<BodyPart> ContainBodyParts => containBodyParts;

		/// <summary>
		/// Flag for if the sprite for this body type changes with gender, true means it does
		/// </summary>
		[Tooltip("Does the sprite change depending on Gender?")]
		[SerializeField] private bool isDimorphic = false;

		/// <summary>
		/// The body part 'container' to which this body part belongs, (eg legs group, arms group), if any
		/// </summary>
		[Tooltip("The 'container' to which this belongs (legs group, arms group, etc), if any")]
		public RootBodyPartContainer Root;

		/// <summary>
		/// The body part in which this body part is contained, if any
		/// </summary>
		[Tooltip("The body part in which this body part is contained, if any")]
		public BodyPart ContainedIn;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents. " +
				 "It is intended for things like arms/legs/heads. " +
				 "Leave empty if it shouldn't change this.")]
		private BodyTypesWithOrder BodyTypesSprites = new BodyTypesWithOrder();

		/// <summary>
		/// The list of sprites associated with this body part
		/// </summary>
		[Tooltip("Sprites associated wtih this part, generated when part is initialized/changed")]
		public List<BodyPartSprites> RelatedPresentSprites = new List<BodyPartSprites>();

		/// <summary>
		/// The final sprite data for this body part accounting for body type and gender
		/// </summary>
		public ListSpriteDataSOWithOrder LimbSpriteData { get; private set; }

		/// <summary>
		/// The prefab sprites for this body part
		/// </summary>
		[Tooltip("The prefab sprites for this")]
		public BodyPartSprites SpritePrefab;

		[Tooltip("The body part's pickable item's sprites.")]
		public SpriteHandler BodyPartItemSprite;

		[Tooltip("Does this body part share the same color as the player's skintone when it deattatches from his body?")]
		public bool BodyPartItemInheritsSkinColor = false;

		/// <summary>
		/// Boolean for whether the sprites for the body part have been set, returns true when they are
		/// </summary>
		[HideInInspector] public bool BodySpriteSet = false;

		/// <summary>
		/// Custom settings from the lobby character designer
		/// </summary>
		[Tooltip("Custom options from the Character Customizer that modifys this")]
		public BodyPartCustomisationBase LobbyCustomisation;

		[Tooltip("List of optional body added to this, eg what wings a Moth has")]
		[SerializeField] private List<BodyPart> optionalOrgans = new List<BodyPart>();
		/// <summary>
		/// The list of optional body that are attached/stored in this body part, eg what wings a Moth has
		/// </summary>
		public List<BodyPart> OptionalOrgans => optionalOrgans;

		/// <summary>
		/// The list of optional body that can be attached/stored in this body part, eg what wings are available on a Moth chest
		/// </summary>
		[Tooltip("List of body parts this can be replaced with")]
		public List<BodyPart> OptionalReplacementOrgan = new List<BodyPart>();

		/// <summary>
		/// Flag that is true if the body part is external (exposed to the outside world), false if it is internal
		/// </summary>
		[Tooltip("Is the body part on the surface?")]
		public bool IsSurface = false;

		[Tooltip("Does the player die when this part gets removed from their body?")]
		public bool DeathOnRemoval = false;

		/// <summary>
		/// Flag to hide clothing on this body part
		/// </summary>
		[Tooltip("Should clothing be hidden on this?")]
		public ClothingHideFlags ClothingHide;

		/// <summary>
		/// What is this BodyPart's sprite's tone if it shared a skin tone with the player?
		/// </summary>
		[HideInInspector] public Color Tone = Color.white;

		[System.NonSerialized]
		public List<BodyPartModification> BodyPartModifications = new List<BodyPartModification>();

		public string SetCustomisationData;

		private bool SystemSetup = false;

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

			UpdateIcons();
			SetUpSystemsThis();
			foreach (var bodyPartModification in BodyPartModifications)
			{
				bodyPartModification.HealthMasterSet();
			}
			//TODO Make this generic \/ for mobs
			Storage.SetRegisterPlayer(healthMaster?.GetComponent<RegisterPlayer>());
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
			Storage = GetComponent<ItemStorage>();
			Storage.ServerInventoryItemSlotSet += ImplantAdded;
			health = maxHealth;
			DamageInitialisation();
			UpdateSeverity();

			BodyPartModifications = this.GetComponents<BodyPartModification>().ToList();

			foreach (var bodyPartModification in BodyPartModifications)
			{
				bodyPartModification.RelatedPart = this;
				bodyPartModification.Initialisation();
			}
		}

		public void ImplantUpdate()
		{
			foreach (BodyPart prop in ContainBodyParts)
			{
				prop.ImplantUpdate();
			}
		}

		/// <summary>
		/// Updates the body part and all contained body parts relative to their related
		/// systems (default: blood system, radiation damage)
		/// </summary>
		public virtual void ImplantPeriodicUpdate()
		{
			foreach (BodyPart prop in ContainBodyParts)
			{
				prop.ImplantPeriodicUpdate();
			}
			BloodUpdate();
			CalculateRadiationDamage();

			foreach (var bodyPartModification in BodyPartModifications)
			{
				bodyPartModification.ImplantPeriodicUpdate();
				if (IsBleedingInternally)
				{
					bodyPartModification.InternalDamageLogic();
				}
			}
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
		/// contents change, and the parent tell the parent's parent and so on until it reaches the highest container.

		/// <summary>
		/// Adds an object to the body part's internal storage, usually another body part
		/// </summary>
		/// <param name="IngameObject">Object to try and store in the Body Part</param>
		public virtual void AddBodyPart(GameObject IngameObject)
		{
			Storage.ServerTryAdd(IngameObject);
		}

		/// <summary>
		/// Transfers an item from an item slot to the body part's internal storage, usually another body part
		/// </summary>
		/// <param name="ItemSlot">Item Slot to transfer from</param>
		public virtual void AddBodyPartSlot(ItemSlot ItemSlot)
		{
			Storage.ServerTryTransferFrom(ItemSlot);
		}

		/// <summary>
		/// Removes the Body Part Item from the storage of its parent (a body part container or another body part)
		/// Will check if the this body part causes death upon removal and will tint it's Item Sprite to the character's skinTone if allowed.
		/// </summary>
		[ContextMenu("Debug - Drop this Body Part")]
		public virtual void RemoveFromBodyThis()
		{
			if (BodyPartRemovalChecks() == false) return;
			dynamic parent = this.GetParent();
			if (parent != null)
			{
				parent.RemoveSpecifiedFromThis(this.gameObject);
			}
		}


		/// <summary>
		/// Checks if it's possible to remove this body part and runs any logic
		/// required upon it's removal.
		/// </summary>
		/// <returns>True if allowed to remove. Flase if gibbing.</returns>
		private bool BodyPartRemovalChecks()
		{
			//Checks if the body part is not an internal organ and if that part shares a skin tone.
			if(IsSurface && BodyPartItemInheritsSkinColor && currentBurnDamageLevel != BurnDamageLevels.CHARRED)
			{
				CharacterSettings settings = HealthMaster.gameObject.Player().Script.characterSettings;
				ColorUtility.TryParseHtmlString(settings.SkinTone, out Tone);
				BodyPartItemSprite.OrNull()?.SetColor(Tone);
			}
			if(currentBurnDamageLevel == BurnDamageLevels.CHARRED)
			{
				BodyPartItemSprite.OrNull()?.SetColor(bodyPartColorWhenCharred);
			}
			//Fixes an error where externally bleeding body parts would continue to try bleeding even after their removal.
			if(IsBleedingExternally)
			{
				StopExternalBleeding();
			}
			if (gibsEntireBodyOnRemoval)
			{
				healthMaster.Gib();
				return false;
			}
			//If this body part is necessary for a character existence, kill them upon removal.
			if(DeathOnRemoval)
			{
				healthMaster.Death();
			}
			return true;
		}

		/// <summary>
		/// Removes a specified item from the body part's storage
		/// </summary>
		/// <param name="inOrgan">Item to remove</param>
		public virtual void RemoveSpecifiedFromThis(GameObject inOrgan)
		{
			Storage.ServerTryRemove(inOrgan);
		}

		/// <summary>
		/// Removes this body part from its host body system
		/// </summary>
		/// <param name="livingHealthMasterBase">Body to be removed from</param>
		public virtual void RemovedFromBody(LivingHealthMasterBase livingHealthMasterBase)
		{
			SubBodyPartRemoved(this);
			foreach (var bodyPartModification in BodyPartModifications)
			{
				bodyPartModification.RemovedFromBody(livingHealthMasterBase);
			}
		}

		/// <summary>
		/// Gets the part of the body that the body part resides in (a body part container or another body part)
		/// </summary>
		/// <returns>A body part that contains it OR a body part container that contains it OR null if it is not
		/// contained in anything</returns>
		///TODO change to some type of inheritance/Interface model
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
		/// Ensures the health master of all sub body parts are the same as their parent, and that the
		/// health master and body part containers know of all contained body parts
		/// </summary>
		/// <param name="implant">Body Part to be initialized</param>
		public void SetUpBodyPart(BodyPart implant)
		{
			implant.HealthMaster = HealthMaster;
			if (HealthMaster == null) return;
			HealthMaster.AddNewImplant(implant);
			SubBodyPartAdded(implant);

		}

		/// <summary>
		/// Sets up the body part to be connected to the internal systems of the body (circulation, respiration, etc)
		/// </summary>
		public virtual void SetUpSystems()
		{
			foreach (BodyPart prop in ContainBodyParts)
			{
				prop.SetUpSystems();
			}

			SetUpSystemsThis();
		}


		public void SetUpSystemsThis()
		{
			if (SystemSetup) return;
			SystemSetup = true;
			BloodInitialise();
			foreach (var bodyPartModification in BodyPartModifications)
			{
				bodyPartModification.SetUpSystems();
			}
		}

		/// <summary>
		/// Adds a new body part to this body part, and removes the old part whose place is
		/// being taken if possible
		/// </summary>
		/// <param name="prevImplant">Old body part to be removed</param>
		/// <param name="newImplant">New body part to be added</param>
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

	[Serializable]
	public class BodyTypesWithOrder
	{
		public SpriteOrder SpriteOrder;

		[Tooltip("NonBinary, male, female, Other1, other2 , ect.")]
		public List<ListSpriteDataSO> BodyTypes = new List<ListSpriteDataSO>();
	}

	[Serializable]
	public class ListSpriteDataSO
	{
		public List<SpriteDataSO> Sprites = new List<SpriteDataSO>();
	}

	[Serializable]
	public class ListSpriteDataSOWithOrder
	{
		public SpriteOrder SpriteOrder;
		public List<SpriteDataSO> Sprites = new List<SpriteDataSO>();
	}
}
