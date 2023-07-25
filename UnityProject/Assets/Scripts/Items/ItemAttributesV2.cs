using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using AddressableReferences;
using Systems.Clothing;
using Chemistry.Components;
using Clothing;
using Items.Others;
using Items.PDA;
using Objects.Atmospherics;
using UI.Action;

namespace Items
{
	/// <summary>
	/// Various attributes associated with a particular item.
	/// New and improved, removes need for UniCloth type stuff, works
	/// well with using prefab variants.
	/// </summary>
	[RequireComponent(typeof(Pickupable))] //Inventory interaction
	[RequireComponent(typeof(RegisterItem))] //Registry with subsistence
	public class ItemAttributesV2 : Attributes
	{
		[Header("Item Info")]

		[SerializeField]
		[Tooltip("Initial traits of this item on spawn.")]
		private List<ItemTrait> initialTraits = null;
		public List<ItemTrait> InitialTraits => initialTraits;


		[Header("Item Damage")]

		[Tooltip("Damage when we click someone with harm intent")]
		[Range(0, 100)]
		[SerializeField]
		private float hitDamage = 0;

		/// <summary>
		/// Damage when we click someone with harm intent, tracked server side only.
		/// </summary>
		public float ServerHitDamage
		{
			get
			{
				//If item has an ICustomDamageCalculation component, use that instead.
				ICustomDamageCalculation part = GetComponent<ICustomDamageCalculation>();
				if (part != null)
				{
					return part.ServerPerformDamageCalculation();
				}

				return hitDamage;
			}
			set => hitDamage = value;
		}

		[Tooltip("Type of damage done when this is thrown or used for melee.")]
		[SerializeField]
		private DamageType damageType = DamageType.Brute;
		/// <summary>
		/// Type of damage done when this is thrown or used for melee, tracked server side only.
		/// </summary>
		public DamageType ServerDamageType
		{
			get => damageType;
			set => damageType = value;
		}

		[Tooltip("How painful it is when someone throws it at you")]
		[Range(0, 100)]
		[SerializeField]
		private float throwDamage = 0;
		/// <summary>
		/// Amout of damage done when this is thrown, tracked server side only.
		/// </summary>
		public float ServerThrowDamage
		{
			get => throwDamage;
			set => throwDamage = value;
		}

		[SerializeField,
		Range(0, 100),
		Tooltip("How likely is this item going to cause tramuatic damage? 0% to disable this.")]
		private float traumaDamageChance = 0;

		public float TraumaDamageChance
		{
			get => traumaDamageChance;
			set => traumaDamageChance = value;
		}

		[EnumFlag]
		public TraumaticDamageTypes TraumaticDamageType;

		[Header("Sprites/Sounds/Flags/Misc.")]

		[Tooltip("How many tiles to move per 0.1s when being thrown")]
		[SerializeField]
		private float throwSpeed = 2;
		/// <summary>
		/// How many tiles to move per 0.1s when being thrown
		/// </summary>
		public float ThrowSpeed => throwSpeed * 4;

		[Tooltip("Max throw distance")]
		[SerializeField]
		private float throwRange = 7;
		/// <summary>
		/// Max throw distance
		/// </summary>
		public float ThrowRange => throwRange;

		[Tooltip("Sound to be played when we click someone with harm intent")]
		[SerializeField]
		private AddressableAudioSource hitSound = null;


		[Tooltip("How to play sounds.")]
		[SerializeField]
		public SoundItemSettings hitSoundSettings;
		/// <summary>
		/// Sound to be played when we click someone with harm intent, tracked server side only
		/// </summary>
		public AddressableAudioSource ServerHitSound
		{
			get => hitSound;
			set => hitSound = value;
		}

		[Tooltip("Sound to be played when object gets added to storage.")]
		[SerializeField]
		private AddressableAudioSource inventoryMoveSound = null;
		public AddressableAudioSource InventoryMoveSound => inventoryMoveSound;

		[Tooltip("Sound to be played when object gets added to storage.")]
		[SerializeField]
		private AddressableAudioSource inventoryRemoveSound = null;
		public AddressableAudioSource InventoryRemoveSound => inventoryRemoveSound;

		//TODO: tank / eva fields should probably be migrated to a different component as they are very specific to clothing, particularly
		//suits and masks. Probably belong in the Clothing component.
		[Tooltip("Is this a mask that can connect to a tank?")]
		[SerializeField]
		private bool canConnectToTank = false;
		/// <summary>
		/// Whether this item can connect to a gas tank.
		/// </summary>
		public bool CanConnectToTank => canConnectToTank;


		[Tooltip("Can this item protect humans against spess?")]
		[SerializeField]
		private bool isEVACapable = false;
		/// <summary>
		/// Can this item protect humans against spess?
		/// </summary>
		public bool IsEVACapable => isEVACapable;

		[Tooltip("Possible verbs used to describe the attack when this is used for melee.")]
		[SerializeField]
		private List<string> attackVerbs;
		/// <summary>
		/// Possible verbs used to describe the attack when this is used for melee, tracked server side only.
		/// </summary>
		public IEnumerable<string> ServerAttackVerbs
		{
			get => attackVerbs;
			set => attackVerbs = new List<string>(value);
		}

		/// <summary>
		/// Actual current traits, accounting for dynamic add / remove. Note that these adds / removes
		/// are not currently synced between client / server.
		/// </summary>
		private HashSet<ItemTrait> traits = new HashSet<ItemTrait>();

		private bool hasInit;

		public bool CanBeUsedOnSelfOnHelpIntent = false;


		public ItemsSprites ItemSprites => itemSprites;

		[Tooltip("The In hands Sprites If it has any")]
		[SerializeField]
		private ItemsSprites itemSprites;

		[SyncVar(hook = nameof(FakeItemSync))]
		public bool IsFakeItem = false;

		[Client]
		private void FakeItemSync(bool oldValue, bool newValue)
		{
			if (newValue)
			{
				IsFakeItem = true;
				var fakeClothes = this;

				if (fakeClothes.TryGetComponent<WearableArmor>(out var armor))
				{
					foreach (var bodyArmr in armor.ArmoredBodyParts)
					{
						bodyArmr.Armor.Melee = 0;
						bodyArmr.Armor.Bullet = 0;
						bodyArmr.Armor.Laser = 0;
						bodyArmr.Armor.Energy = 0;
						bodyArmr.Armor.Bomb = 0;
						bodyArmr.Armor.Rad = 0;
						bodyArmr.Armor.Fire = 0;
						bodyArmr.Armor.Acid = 0;
						bodyArmr.Armor.Magic = 0;
						bodyArmr.Armor.Bio = 0;
						bodyArmr.Armor.Anomaly = 0;
						bodyArmr.Armor.DismembermentProtectionChance = 0;
						bodyArmr.Armor.StunImmunity = false;
						bodyArmr.Armor.TemperatureProtectionInK = new Vector2(283.15f, 283.15f + 20);
						bodyArmr.Armor.PressureProtectionInKpa = new Vector2(30f, 300f);

					}
				}
				//if (fakeClothes.TryGetComponent<IDCard>(out var card))
				//{
				//	var cardName = card.GetJobTitle();
				//	var cardRagName = card.RegisteredName;
				//	card.ServerChangeOccupation(OccupationList.Instance.Get(JobType.ASSISTANT), false, true);
				//	card.SyncJobTitle("", cardName);
				//	card.SyncName("", cardRagName);
				//	for (int i = 0; i < card.currencies.Length; i++)
				//	{
				//		card.currencies[i] = 0;
				//	}
				//}
				else if (fakeClothes.TryGetComponent<Headset>(out var headset))
				{
					Destroy(headset);
					//Destroy(headset);
					//headset.IsPowered = false;
					//headset.EncryptionKey = EncryptionKeyType.None;
					//headset.EmmitableSignalData.Clear();
				}
				else if (fakeClothes.TryGetComponent<NightVisionGoggles>(out var nightVision))
				{
					Destroy(nightVision);
				}
				else if (fakeClothes.TryGetComponent<PrescriptionGlasses>(out var glasses))
				{
					Destroy(glasses);
				}
				if (fakeClothes.TryGetComponent<ReagentContainer>(out var container))
				{
					Destroy(container);
				}
				if (fakeClothes.TryGetComponent<ItemActionButton>(out var actionButton))
				{
					//actionButton.OnRemovedFromBody(this);
					Destroy(actionButton);
				}
				if (fakeClothes.TryGetComponent<GasContainer>(out var gasContainer))
				{
					Destroy(gasContainer);
					//gasContainer.NetDisable();
				}
				if (fakeClothes.TryGetComponent<EmergencyOxygenTank>(out var emergencyOxygenTank))
				{
					Destroy(emergencyOxygenTank);
					//emergencyOxygenTank.NetDisable();
				}
				if (fakeClothes.TryGetComponent<PDALogic>(out var pda))
				{
					pda.NetDisable();
					Destroy(pda);
					Destroy(fakeClothes.GetComponent<HasNetworkTabItem>());
					Destroy(fakeClothes.GetComponent<PDANotesNetworkHandler>());
				}
				if (fakeClothes.TryGetComponent<ItemLightControl>(out var lightControl))
				{
					Destroy(lightControl);
				}
				if (fakeClothes.TryGetComponent<FlashLight>(out var flashLight))
				{
					Destroy(flashLight);
					flashLight.NetDisable();
				}
				if (fakeClothes.TryGetComponent<InteractableStorage>(out var intStorage))
				{
					intStorage.NetDisable();
					Destroy(intStorage);
				}
			}
		}

		#region Lifecycle

		private void Awake()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (hasInit) return;
			foreach (var definedTrait in initialTraits)
			{
				traits.Add(definedTrait);
			}

			hasInit = true;
		}

		public override void OnStartClient()
		{
			EnsureInit();
			base.OnStartClient();
		}


		#endregion Lifecycle

		/// <summary>
		/// All traits currently on the item.
		/// NOTE: Dynamically added / removed traits are not synced between client / server
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ItemTrait> GetTraits()
		{
			return traits;
		}


		/// <summary>
		/// Does it have the given trait?
		/// NOTE: Dynamically added / removed traits are not synced between client / server
		/// </summary>
		/// <param name="itemTrait"></param>
		public bool HasTrait(ItemTrait toCheck)
		{
			return traits.Contains(toCheck);
		}

		/// <summary>
		/// Does it have any of the given traits?
		/// </summary>
		/// <param name="expectedTraits"></param>
		/// <returns></returns>
		public bool HasAnyTrait(IEnumerable<ItemTrait> expectedTraits)
		{
			return traits.Any(expectedTraits.Contains);
		}

		/// <summary>
		/// Does it have all of the given traits?
		/// </summary>
		/// <param name="expectedTraits"></param>
		/// <returns></returns>
		public bool HasAllTraits(IEnumerable<ItemTrait> expectedTraits)
		{
			foreach (var required in expectedTraits)
			{
				if (traits.Contains(required) == false)
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Adds the trait dynamically.
		/// NOTE: Not synced between client / server
		/// </summary>
		/// <param name="itemTrait"></param>
		public void AddTrait(ItemTrait toAdd)
		{
			traits.Add(toAdd);
		}



		/// <summary>
		/// Removes the trait dynamically.
		/// NOTE: Not synced between client / server
		/// </summary>
		/// <param name="itemTrait"></param>
		public void RemoveTrait(ItemTrait toRemove)
		{
			traits.Remove(toRemove);
		}

		private static string GetMasterTypeHandsString(SpriteType masterType)
		{
			switch (masterType)
			{
				case SpriteType.Clothing: return "clothing";

				default: return "items";
			}
		}

		/// <summary>
		/// Use SpriteHandlerController.SetSprites instead. (SpriteHandlerController may now be deprecated)
		/// </summary>
		/// <param name="newSprites">New sprites</param>
		public void SetSprites(ItemsSprites newSprites)
		{
			itemSprites = newSprites;
		}

		[ContextMenu("Propagate Palette Changes")]
		public void PropagatePaletteChanges()
		{
			ClothingV2 clothing = GetComponent<ClothingV2>();
			if (clothing != null) clothing.AssignPaletteToSprites(this.ItemSprites.Palette);
		}


	}

	public enum SoundItemSettings
	{
		Both = 0,
		OnlyItem = 1,
		OnlyObject = 2
	}
}
