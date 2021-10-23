using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Systems.Clothing;
using Mirror;
using NaughtyAttributes;
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
		public LivingHealthMasterBase HealthMaster {get; private set;}


		//TODO: prefab populator still contains bodyparts
		/// <summary>
		/// Storage container for things (usually other body parts) held within this body part
		/// </summary>
		[HorizontalLine]
		[Tooltip("Things (eg other organs) held within this")]
		public ItemStorage OrganStorage = null;

		[NonSerialized]
		public List<Organ> OrganList = new List<Organ>();

		/// <summary>
		/// Player sprites for rendering equipment and clothing on the body part container
		/// </summary>
		[Tooltip("Player sprites for rendering equipment and clothing on this")]
		public PlayerSprites playerSprites;

		[HideInInspector] public bool IsBleeding = false;

		/// <summary>
		/// How much blood does the body lose when there is lost limbs in this container?
		/// </summary>
		[SerializeField, Tooltip("How much blood does the body lose when there is lost limbs in this container?")]
		private float limbLossBleedingValue = 3.5f;

		/// <summary>
		/// The category that this body part falls under for targeting purposes
		/// </summary>
		[Tooltip("The category that this body part falls under for targeting purposes")]
		[SerializeField] public BodyPartType BodyPartType;

		/// <summary>
		/// Flag for if the sprite for this body type changes with gender, true means it does
		/// </summary>
		[Tooltip("Does the sprite change depending on Gender?")]
		[SerializeField] private bool isDimorphic = false;

		/// <summary>
		/// The body part in which this body part is contained, if any
		/// </summary>
		[Tooltip("The body part in which this body part is contained, if any")]
		[HideInInspector] public BodyPart ContainedIn;

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

		public string SetCustomisationData;

		private bool SystemSetup = false;

		public IntName intName;

		void Awake()
		{
			OrganStorage = GetComponent<ItemStorage>();
			OrganStorage.ServerInventoryItemSlotSet += OrganTransfer;
			health = maxHealth;
			AddModifier(DamageModifier);
			UpdateSeverity();
		}

		/// <summary>
		/// Updates the body part and all contained body parts relative to their related
		/// systems (default: blood system, radiation damage)
		/// </summary>
		public void ImplantPeriodicUpdate()
		{
			for (int i = OrganList.Count - 1; i >= 0; i--)
			{
				var organ = OrganList[i];
				organ.ImplantPeriodicUpdate();
				if (IsBleedingInternally)
				{
					organ.InternalDamageLogic();
				}
			}
			BloodUpdate();
			CalculateRadiationDamage();

			if(IsBleeding)
			{
				HealthMaster.CirculatorySystem.Bleed(limbLossBleedingValue);
			}
		}

		public void SetHealthMaster(LivingHealthMasterBase livingHealth)
		{
			HealthMaster = livingHealth;
			if (livingHealth)
			{
				playerSprites = livingHealth.GetComponent<PlayerSprites>();
			}
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


			var dynamicItemStorage = HealthMaster.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				var bodyPartUISlots = GetComponent<BodyPartUISlots>();
				dynamicItemStorage.Add(bodyPartUISlots);
			}


			//TODO Make this generic \/ for mobs
			OrganStorage.SetRegisterPlayer(HealthMaster?.GetComponent<RegisterPlayer>());
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

		//TODO: confusing, make it not depend from the inventory storage Action
		/// <summary>
		/// Both addition and removal of an organ
		/// </summary>
		public void OrganTransfer(Pickupable prevImplant, Pickupable newImplant)
		{
			if (newImplant && newImplant.TryGetComponent<Organ>(out var addedOrgan))
			{
				addedOrgan.RelatedPart = this;
				OrganList.Add(addedOrgan);
				addedOrgan.Initialisation();

				if (HealthMaster)
				{
					//TODO: horrible, remove -- organ prefabs have a bodypart component
					var bodyPart = addedOrgan.GetComponent<BodyPart>();
					HealthMaster.ServerCreateSprite(bodyPart);
				}
			}
			else if(prevImplant && prevImplant.TryGetComponent<Organ>(out var removedOrgan))
			{
				OrganList.Remove(removedOrgan);
				removedOrgan.RemovedFromBody(HealthMaster);
				removedOrgan.RelatedPart = null;
			}
		}

		/// <summary>
		/// Body part was added to the body
		/// </summary>
		public void BodyPartAdded(LivingHealthMasterBase livingHealth)
		{
			livingHealth.BodyPartList.Add(this);
			SetHealthMaster(livingHealth);
			livingHealth.ServerCreateSprite(this);

			//legs and arms getting ready to affect speed
			if (TryGetComponent<Limb>(out var limb))
			{
				limb.Initialize();
			}

			//TODO: horrible, remove -- organ prefabs have bodyparts
			foreach (var organ in OrganList)
			{
				var organBodyPart = organ.GetComponent<BodyPart>();
				livingHealth.ServerCreateSprite(organBodyPart);
			}
		}

		/// <summary>
		/// Body part was removed from the body
		/// </summary>
		public void BodyPartRemoval()
		{
			foreach (var organ in OrganList)
			{
				organ.RemovedFromBody(HealthMaster);

				//TODO: horrible, remove -- organ prefabs have bodyparts
				var organBodyPart = organ.GetComponent<BodyPart>();
				organBodyPart.RemoveSprites(playerSprites, HealthMaster);
			}
			RemoveSprites(playerSprites, HealthMaster);
			HealthMaster.rootBodyPartController.UpdateClients();
			HealthMaster.BodyPartList.Remove(this);
		}

		/// <summary>
		/// Server only - Tries to remove a body part
		/// </summary>
		public void TryRemoveFromBody(bool beingGibbed = false)
		{
			SetRemovedColor();
			foreach (var bodyPart in HealthMaster.BodyPartList)
			{
				if (bodyPart.BodyPartType == BodyPartType.Chest)
				{
					bodyPart.IsBleeding = true;
				}
			}

			DropItemsOnDismemberment(this);
			HealthMaster.BodyPartStorage.ServerTryRemove(gameObject);
			var bodyPartUISlot = GetComponent<BodyPartUISlots>();
			var dynamicItemStorage = HealthMaster.GetComponent<DynamicItemStorage>();
			dynamicItemStorage.Remove(bodyPartUISlot);
			//Fixes an error where externally bleeding body parts would continue to try bleeding even after their removal.
			if(IsBleedingExternally)
			{
				StopExternalBleeding();
			}
			//this kills the crab
			if(DeathOnRemoval)
			{
				HealthMaster.Death();
			}
			if (gibsEntireBodyOnRemoval && beingGibbed == false)
			{
				HealthMaster.Gib();
			}
		}

		/// <summary>
		/// Drops items from a player's bodyPart inventory upon dismemberment.
		/// </summary>
		/// <param name="bodyPart">The bodyPart that's cut off</param>
		private void DropItemsOnDismemberment(BodyPart bodyPart)
		{
			DynamicItemStorage storge = HealthMaster.playerScript.DynamicItemStorage;

			void RemoveItemsFromSlot(NamedSlot namedSlot)
			{
				foreach (ItemSlot itemSlot in storge.GetNamedItemSlots(namedSlot))
				{
					Inventory.ServerDrop(itemSlot);
				}
			}

			//We remove items from both hands to simulate a pain effect, because usually when you lose your arm the other one goes into shock
			if (bodyPart.BodyPartType == BodyPartType.LeftArm
			    || bodyPart.BodyPartType == BodyPartType.RightArm || bodyPart.BodyPartType == BodyPartType.LeftHand
			    || bodyPart.BodyPartType == BodyPartType.RightHand || bodyPart.DeathOnRemoval)
			{
				RemoveItemsFromSlot(NamedSlot.leftHand);
				RemoveItemsFromSlot(NamedSlot.rightHand);
			}
			if(bodyPart.BodyPartType == BodyPartType.RightLeg || bodyPart.BodyPartType == BodyPartType.LeftLeg ||
			   bodyPart.BodyPartType == BodyPartType.LeftFoot || bodyPart.BodyPartType == BodyPartType.RightFoot)
			{
				RemoveItemsFromSlot(NamedSlot.feet);
			}

			if (bodyPart.BodyPartType == BodyPartType.Head)
			{
				RemoveItemsFromSlot(NamedSlot.head);
			}
		}


		#region BodyPartStorage

		/// <summary>
		/// Sets the color of the body part item that is removed
		/// </summary>
		private void SetRemovedColor()
		{
			if(IsSurface && BodyPartItemInheritsSkinColor && currentBurnDamageLevel != TraumaDamageLevel.CRITICAL)
			{
				CharacterSettings settings = HealthMaster.gameObject.Player().Script.characterSettings;
				ColorUtility.TryParseHtmlString(settings.SkinTone, out Tone);
				BodyPartItemSprite.OrNull()?.SetColor(Tone);
			}
			if(currentBurnDamageLevel == TraumaDamageLevel.CRITICAL)
			{
				BodyPartItemSprite.OrNull()?.SetColor(bodyPartColorWhenCharred);
			}
		}


		private void RemoveSprites(PlayerSprites sprites, LivingHealthMasterBase livingHealth)
		{
			for (var i = RelatedPresentSprites.Count - 1; i >= 0; i--)
			{
				var bodyPartSprite = RelatedPresentSprites[i];
				if (IsSurface)
				{
					sprites.SurfaceSprite.Remove(bodyPartSprite);
				}
				RelatedPresentSprites.Remove(bodyPartSprite);
				sprites.Addedbodypart.Remove(bodyPartSprite);
				Destroy(bodyPartSprite.gameObject);
			}
			livingHealth.InternalNetIDs.Remove(intName);
		}

		public void SetUpSystemsThis()
		{
			if (SystemSetup) return;
			SystemSetup = true;
			BloodInitialise();
			foreach (var organ in OrganList)
			{
				organ.SetUpSystems();
			}
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
