using US13.HealthV2.Living;
using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.Clothing;
using US13.Core.Utils;
using US13.Items;
using US13.Items.Implants.Organs;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Systems.Lobby;
using US13.UI.Systems.Lobby.SubCustomisation.BodyPartCustomisations;
using Util;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.HealthV2.Living.BodyParts
{
	/// <summary>
	/// A part of a body. Can be external, such as a limb, or internal like an organ.
	/// Body parts can also contain other body parts, e.g. the 'brain body part' contained in the 'head body part'.
	/// BodyPart is a partial class split into BodyPart, BodyPartDamage, BodyPartBlood, BodyPartSurgery, and BodyPartModifiers.
	/// </summary>
	public partial class BodyPart : MonoBehaviour, IBodyPartDropDownOrgans
	{
		public LivingHealthMasterBase HealthMaster { get; private set; }


		[HideInInspector] private readonly List<BodyPart> containBodyParts = new List<BodyPart>();
		public event Action<LivingHealthMasterBase> OnAddedToBody;
		public event Action<LivingHealthMasterBase> OnRemovedFromBody;

		public List<BodyPart> ContainBodyParts => containBodyParts;

		[Tooltip("If marked as cybernetic, unless the host body is a cyborg, the bodyPartType will not effect this parts sprites"),SerializeField]
		private bool isCybernetic = false;


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
		[NonSerialized] public List<IBodyPartBehavior> OrganList = new List<IBodyPartBehavior>();

		/// <summary>
		/// Player sprites for rendering equipment and clothing on the body part container
		/// </summary>
		[Tooltip("Player sprites for rendering equipment and clothing on this")]
		[HideInInspector] public PlayerSprites playerSprites;

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

		public BodyTypesWithOrder GetBodyTypesSprites => BodyTypesSprites;

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

		private BodyPartBehaviors behaviorHost;
		private bool systemsRegistered;
		private bool removingFromBody;

		public ItemAttributesV2 ItemAttributes;




		void Awake()
		{
			CommonComponents = GetComponent<CommonComponents>();
			ItemAttributes = GetComponent<ItemAttributesV2>();
			OrganStorage = GetComponent<ItemStorage>();
			if (OrganStorage != null) OrganStorage.ServerInventoryItemSlotSet += BodyPartTransfer;
			InitializeBehaviors();

			health = maxHealth;
			AddModifier(DamageModifier);
			UpdateSeverity();
		}

		private void InitializeBehaviors()
		{
			if (behaviorHost == null) behaviorHost = GetComponent<BodyPartBehaviors>();
			if (behaviorHost == null) return;
			behaviorHost.Initialize();
			// Preserve the lookup API without maintaining a second callback collection.
			OrganList = behaviorHost.Behaviors;
		}

		private void OnDestroy()
		{
			if (OrganStorage != null) OrganStorage.ServerInventoryItemSlotSet -= BodyPartTransfer;
			BodyPartRemoveHealthMaster();
		}

		/// <summary>
		/// Updates the body part and all contained body parts relative to their related
		/// systems (default: blood system, radiation damage)
		/// </summary>
		public void ImplantPeriodicUpdate()
		{
			if (HealthMaster == null) return;
			behaviorHost?.BehaviorsTick();
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

			var dynamicItemStorage = HealthMaster.GetComponent<DynamicItemStorage>();
			if (dynamicItemStorage != null)
			{
				try
				{
					var bodyPartUISlots = GetComponent<BodyPartUISlots>();
					if (bodyPartUISlots != null)
					{
						dynamicItemStorage.Add(bodyPartUISlots);
					}
				}
				catch (Exception e)
				{
					Loggy.Error("An error occured when adding body UI slots:\n" + e.ToString());
				}
			}


			//TODO Make this generic \/ for mobs
			if (OrganStorage != null) OrganStorage.SetRegisterPlayer(HealthMaster?.GetComponent<RegisterPlayer>());
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
			if (livingHealth == null || HealthMaster == livingHealth) return;
			if (HealthMaster != null) BodyPartRemoveHealthMaster();
			InitializeBehaviors();
			SetHealthMaster(livingHealth);
			livingHealth.AddingBodyPart(this, notifySystems: false);
			BodyType bodyType = BodyType.NonBinary;
			if (isCybernetic && playerSprites != null && playerSprites.ThisCharacter?.Species != "Cyborg") bodyType = BodyType.Other2; //Synthetic
			else if (playerSprites != null && playerSprites.ThisCharacter != null) //This check is important, otherwise cyborg limbs on humans will change role based on gender
			{
				bodyType = playerSprites.ThisCharacter.BodyType;
			}

			ServerCreateSprite(bodyType);

			foreach (var organ in OrganList)
			{
				if (organ == null) continue;
				var organType = organ.GetType();
				while (organType != null && organType != typeof(BodyPartBehavior) && organType != typeof(object))
				{
					livingHealth.AddOrgan(organType,organ);
					organType = organType.BaseType;
				}

			}
			behaviorHost?.OnBodyPartAdded(livingHealth);
			systemsRegistered = true;
			livingHealth.NotifyBodyPartAdded(this);

			for (int i = 0; i < containBodyParts.Count; i++) //Only add Body parts
			{
				containBodyParts[i].BodyPartAddHealthMaster(livingHealth);
			}

			livingHealth.BodyPartListChange();

			OnAddedToBody?.Invoke(livingHealth);
		}

		/// <summary>
		/// Body part was removed from the body
		/// </summary>
		public void BodyPartRemoveHealthMaster()
		{
			if (HealthMaster == null || removingFromBody) return;
			removingFromBody = true;
			if (behaviorHost != null) behaviorHost.OnRemovedFromBody(HealthMaster);
			NotifySystemsRemoved(HealthMaster);
			foreach (var organ in OrganList)
			{
				if (organ == null) continue;
				var Type = organ.GetType();
				while (Type != null && Type != typeof(BodyPartBehavior) && Type != typeof(object))
				{
					HealthMaster.RemoveOrgan(Type,organ);
					Type = Type.BaseType;
				}

			}

			foreach (var organ in containBodyParts)
			{
				organ.BodyPartRemoveHealthMaster();
			}

			RemoveAllSprites();
			OnRemovedFromBody?.Invoke(HealthMaster);
			HealthMaster.RemovingBodyPart(this);
			HealthMaster.BodyPartListChange();
			HealthMaster = null;
			removingFromBody = false;
		}

		internal void NotifySystemsRemoved(LivingHealthMasterBase livingHealth)
		{
			if (!systemsRegistered) return;
			systemsRegistered = false;
			livingHealth.NotifyBodyPartRemoved(this);
		}


		public void RemoveInventoryAndBody(Vector3 AppearAtWorld)
		{
			var slot = this.GetCachedComponent<Pickupable>().ItemSlot;
			if (slot != null)
			{
				Inventory.ServerDrop(slot);
			}

			TryRemoveFromBody();
			this.GetCachedComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(AppearAtWorld);
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
					HealthMaster.OnGib(  " I think this is unreachable but keeping it just in case it isn't, being gibbed by Body part remove " );
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
			behaviorHost?.SetUpSystems();
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
