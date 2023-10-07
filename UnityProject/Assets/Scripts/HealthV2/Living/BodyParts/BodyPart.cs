using System;
using System.Collections.Generic;
using Items;
using Items.Implants.Organs;
using Mirror;
using UnityEngine;
using NaughtyAttributes;
using Player;
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
		public LivingHealthMasterBase HealthMaster { get; private set; }


		[HideInInspector] private readonly List<BodyPart> containBodyParts = new List<BodyPart>();
		public List<BodyPart> ContainBodyParts => containBodyParts;


		/// <summary>
		/// Storage container for things (usually other body parts) held within this body part
		/// </summary>
		[HorizontalLine] [Tooltip("Things (eg other organs) held within this")]
		public ItemStorage OrganStorage = null;

		[SerializeField, Tooltip(
			 " If you threw acid onto a player would body parts contained in this body part get touched by the acid, If this body part was on the surface ")]
		private bool isOpenAir = false;

		public bool IsOpenAir
		{
			get
			{
				if (isOpenAir)
				{
					if (ContainedIn == null) return true;

					return ContainedIn.IsOpenAir;
				}

				return false;
			}
		}

		public bool IsInAnOpenAir
		{
			get
			{
				if (ContainedIn == null) return true;
				return ContainedIn.IsOpenAir;
			}
		}


		[HideInInspector] public CommonComponents CommonComponents;

		//Organs on the same body part
		[NonSerialized] public List<BodyPartFunctionality> OrganList = new List<BodyPartFunctionality>();

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
		[Tooltip("The category that this body part falls under for targeting purposes")] [SerializeField]
		public BodyPartType BodyPartType;

		/// <summary>
		/// Flag for if the sprite for this body type changes with gender, true means it does
		/// </summary>
		[Tooltip("Does the sprite change depending on Gender?")] [SerializeField]
		private bool isDimorphic = false;

		/// <summary>
		/// The body part in which this body part is contained, if any
		/// </summary>
		[Tooltip("The body part in which this body part is contained, if any")] [HideInInspector]
		public BodyPart ContainedIn;

		[SerializeField]
		[Tooltip("The visuals of this implant. This will be used for the limb the implant represents. " +
		         "It is intended for things like arms/legs/heads. " +
		         "Leave empty if it shouldn't change this.")]
		private BodyTypesWithOrder BodyTypesSprites = new BodyTypesWithOrder();

		/// <summary>
		/// The list of sprites associated with this body part
		/// </summary>
		[HideInInspector] public List<BodyPartSprites> RelatedPresentSprites = new List<BodyPartSprites>();


		[Tooltip("Does the player die when this part gets removed from their body?")]
		public bool DeathOnRemoval = false;

		/// <summary>
		/// Flag to hide clothing on this body part
		/// </summary>
		[Tooltip("Should clothing be hidden on this?")]
		public ClothingHideFlags ClothingHide;

		public string SetCustomisationData;

		private bool SystemSetup = false;

		public ItemAttributesV2 ItemAttributes;




		void Awake()
		{
			CommonComponents = GetComponent<CommonComponents>();
			ItemAttributes = GetComponent<ItemAttributesV2>();
			OrganStorage = GetComponent<ItemStorage>();
			OrganStorage.ServerInventoryItemSlotSet += BodyPartTransfer;
			OrganList.Clear();
			OrganList.AddRange(this.GetComponents<BodyPartFunctionality>());

			foreach (var Organ in OrganList)
			{
				Organ.RelatedPart = this;
			}

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
			}

			CalculateRadiationDamage();
		}

		public void SetHealthMaster(LivingHealthMasterBase livingHealth)
		{
			HealthMaster = livingHealth;
			if (livingHealth)
			{
				playerSprites = livingHealth.GetComponent<PlayerSprites>();
			}


			UpdateIcons();
			SetUpSystemsThis();

			var dynamicItemStorage = HealthMaster.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				var bodyPartUISlots = GetComponent<BodyPartUISlots>();
				if (bodyPartUISlots != null)
				{
					dynamicItemStorage.Add(bodyPartUISlots);
				}
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

		/// <summary>
		/// Both addition and removal of an organ
		/// </summary>
		public void BodyPartTransfer(Pickupable prevImplant, Pickupable newImplant)
		{
			if (newImplant && newImplant.TryGetComponent<BodyPart>(out var addedOrgan))
			{
				addedOrgan.ContainedIn = this;
				containBodyParts.Add(addedOrgan);

				if (HealthMaster)
				{
					addedOrgan.BodyPartAddHealthMaster(HealthMaster);
				}
			}
			else if (prevImplant && prevImplant.TryGetComponent<BodyPart>(out var removedOrgan))
			{
				containBodyParts.Remove(removedOrgan);

				removedOrgan.ContainedIn = null;
				if (HealthMaster)
				{
					removedOrgan.BodyPartRemoveHealthMaster();
				}
			}
		}

		/// <summary>
		/// Body part was added to the body
		/// </summary>
		public void BodyPartAddHealthMaster(LivingHealthMasterBase livingHealth) //Only add Body parts
		{
			livingHealth.AddingBodyPart(this);

			SetHealthMaster(livingHealth);
			ServerCreateSprite();

			foreach (var organ in OrganList)
			{
				organ.OnAddedToBody(HealthMaster); //Only add Body parts
			}

			for (int i = 0; i < containBodyParts.Count; i++) //Only add Body parts
			{
				containBodyParts[i].BodyPartAddHealthMaster(livingHealth);
			}

			livingHealth.BodyPartListChange();
		}

		/// <summary>
		/// Body part was removed from the body
		/// </summary>
		public void BodyPartRemoveHealthMaster()
		{
			foreach (var organ in OrganList)
			{
				organ.OnRemovedFromBody(HealthMaster);
			}

			foreach (var organ in containBodyParts)
			{
				organ.BodyPartRemoveHealthMaster();
			}

			RemoveAllSprites();
			HealthMaster.RemovingBodyPart(this);
			HealthMaster.BodyPartListChange();
			HealthMaster = null;
		}


		public void RemoveInventoryAndBody(Vector3 AppearAtWorld)
		{
			var slot = this.GetComponentCustom<Pickupable>().ItemSlot;
			if (slot != null)
			{
				Inventory.ServerDrop(slot);
			}

			TryRemoveFromBody();
			this.GetComponentCustom<UniversalObjectPhysics>().AppearAtWorldPositionServer(AppearAtWorld);
		}


		/// <summary>
		/// Server only - Tries to remove a body part
		/// </summary>
		public void TryRemoveFromBody(bool beingGibbed = false, bool CausesBleed = true, bool Destroy = false,
			bool PreventGibb_Death = false) //TODO It should do the stuff automatically when removed from inventory
		{
			if (HealthMaster == null) return;
			bool alreadyBleeding = false;
			if (CausesBleed && HealthMaster != null)
			{
				foreach (var bodyPart in HealthMaster.BodyPartList)
				{
					if (bodyPart.BodyPartType == BodyPartType.Chest && alreadyBleeding == false)
					{
						bodyPart.IsBleeding = true;
						alreadyBleeding = true;
						HealthMaster.ChangeBleedStacks(limbLossBleedingValue);
					}
				}
			}


			DropItemsOnDismemberment(this);


			var bodyPartUISlot = GetComponent<BodyPartUISlots>();

			var dynamicItemStorage = HealthMaster.GetComponent<DynamicItemStorage>();
			dynamicItemStorage.Remove(bodyPartUISlot);

			if (PreventGibb_Death == false)
			{
				//this kills the crab
				if (DeathOnRemoval)
				{
					HealthMaster.Death();
				}

				if (beingGibbed)
				{
					HealthMaster.OnGib();
				}
			}


			if (ContainedIn != null)
			{
				if (beingGibbed)
				{
					ContainedIn.OrganStorage.ServerTryRemove(gameObject, Destroy,
						DroppedAtWorldPositionOrThrowVector: ConverterExtensions.GetRandomRotatedVector2(-0.5f, 0.5f),
						Throw: true);
				}
				else
				{
					ContainedIn.OrganStorage.ServerTryRemove(gameObject, Destroy);
				}
			}
			else
			{
				if (beingGibbed)
				{
					HealthMaster.OrNull()?.BodyPartStorage.OrNull()?.ServerTryRemove(gameObject, Destroy,
						DroppedAtWorldPositionOrThrowVector: ConverterExtensions.GetRandomRotatedVector2(-0.5f, 0.5f),
						Throw: true);
				}
				else
				{
					HealthMaster.OrNull()?.BodyPartStorage.OrNull()?.ServerTryRemove(gameObject, Destroy);
				}
			}
		}

		/// <summary>
		/// Drops items from a player's bodyPart inventory upon dismemberment.
		/// </summary>
		/// <param name="bodyPart">The bodyPart that's cut off</param>
		private void DropItemsOnDismemberment(BodyPart bodyPart)
		{
			if (HealthMaster == null) return;
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

			if (bodyPart.BodyPartType == BodyPartType.RightLeg || bodyPart.BodyPartType == BodyPartType.LeftLeg ||
			    bodyPart.BodyPartType == BodyPartType.LeftFoot || bodyPart.BodyPartType == BodyPartType.RightFoot)
			{
				RemoveItemsFromSlot(NamedSlot.feet);
			}

			if (bodyPart.BodyPartType == BodyPartType.Head)
			{
				RemoveItemsFromSlot(NamedSlot.head);
			}
		}

		public void ChangeBodyPartColor(Color color)
		{
			foreach (var sprite in RelatedPresentSprites)
			{
				sprite.baseSpriteHandler.SetColor(color);
			}
		}


		#region BodyPartStorage

		public void SetUpSystemsThis()
		{
			if (SystemSetup) return;
			SystemSetup = true;

			foreach (var Organ in OrganList)
			{
				Organ.SetUpSystems();
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