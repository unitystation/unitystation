using System;
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using Mirror;
using NaughtyAttributes;

namespace Systems.Clothing
{
	/// <summary>
	/// Indicates that a given item functions as an article of clothing. Contains the data related to the clothing / how it should look.
	/// New and improved, removes need for UniCloth type stuff, works
	/// well with using prefab variants.
	/// </summary>
	public class ClothingV2 : NetworkBehaviour
	{
		[Tooltip("All sets of clothing data for this clothing, e.g. hardhat on and hardhat off.")]
		[SerializeField, BoxGroup("Cloth Data"), ReorderableList]
		private List<ClothingDataV2> allClothingData = default;

		[Tooltip("The index of allClothingData this clothing should default to, if not random.")]
		[SerializeField, BoxGroup("Cloth Data"), DisableIf(nameof(randomInitialClothData))]
		public int CurrentClothIndex = 0;

		[Tooltip("Whether a random cloth data SO will be selected for the initial cloth data.")]
		[SerializeField, BoxGroup("Cloth Data")]
		private bool randomInitialClothData = false;

		[Tooltip("Determine whether this piece of clothing obscures the identity of the wearer (head, maskwear)")]
		[SerializeField]
		private bool hidesIdentity = false;

		[Tooltip("Determine which slots this clothing should obscure.")]
		[SerializeField, EnumFlag]
		private NamedSlotFlagged hidesSlots = NamedSlotFlagged.None;

		[Tooltip("Determine when a piece of clothing hides another")]
		[SerializeField, EnumFlag]
		private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;

		private ItemAttributesV2 myItem;
		private Pickupable pickupable;

		[NonSerialized]
		public List<SpriteDataSO> SpriteDataSO = new List<SpriteDataSO>();
		/// <summary>
		/// Clothing item this is currently equipped to, if there is one. Will be updated when the data is synced.
		/// </summary>
		private ClothingItem clothingItem;

		public ClothingItem ClothingItem => clothingItem;
		public ClothingDataV2 CurrentClothData => allClothingData[CurrentClothIndex];

		/// <summary> Whether this piece of clothing obscures the identity of the wearer (head, maskwear). </summary>
		public bool HidesIdentity => hidesIdentity;

		/// <summary>
		/// When this item is equipped, these are the slots that should be hidden.
		/// </summary>
		public NamedSlotFlagged HiddenSlots => hidesSlots;
		/// <summary>
		/// Determine when a piece of clothing hides another
		/// </summary>
		public ClothingHideFlags HideClothingFlags => hideClothingFlags;

		private void Awake()
		{
			myItem = GetComponent<ItemAttributesV2>();
			pickupable = GetComponent<Pickupable>();
			TryInit();
		}

		//set up the sprites / config of this instance using the cloth data
		private void TryInit()
		{
			foreach (ClothingDataV2 clothData in allClothingData)
			{
				SetUpFromClothingData(clothData);
				SpriteDataSO.Add(clothData.SpriteEquipped);
			}

			if (randomInitialClothData)
			{
				CurrentClothIndex = UnityEngine.Random.Range(0, allClothingData.Count);
			}
		}

		private void SetUpFromClothingData(ClothingDataV2 equippedData)
		{
			var SpriteSOData = new ItemsSprites();
			SpriteSOData.Palette = new List<Color>(equippedData.Palette);
			SpriteSOData.SpriteLeftHand = (equippedData.SpriteInHandsLeft);
			SpriteSOData.SpriteRightHand = (equippedData.SpriteInHandsRight);
			SpriteSOData.SpriteInventoryIcon = (equippedData.SpriteItemIcon);
			SpriteSOData.IsPaletted = equippedData.IsPaletted;

			myItem.SetSprites(SpriteSOData);
		}

		public void AssignPaletteToSprites(List<Color> palette)
		{
			if (myItem.ItemSprites.IsPaletted)
			{
				//	myItem.SetPaletteOfCurrentSprite(palette);
				clothingItem?.spriteHandler.SetPaletteOfCurrentSprite(palette, networked: false);
				pickupable.SetPalette(palette);
			}
		}

		public void LinkClothingItem(ClothingItem clothingItem)
		{
			this.clothingItem = clothingItem;
			RefreshClothingItem();
		}

		private void RefreshClothingItem()
		{
			if (clothingItem != null)
			{
				clothingItem.RefreshFromClothing(this);
			}
		}

		public void ChangeSprite(int index)
		{
			if (index >= allClothingData.Count) return;
			CurrentClothIndex = index;
			RefreshClothingItem();
		}

		public List<Color> GetPaletteOrNull()
		{
			return CurrentClothData == null ? null : CurrentClothData.GetPaletteOrNull(0);
		}
	}

	/// <summary>
	/// Bit flags which determine when a piece of clothing hides another visually.
	/// IE a helmet hiding glasses.
	/// </summary>
	[Flags]
	public enum ClothingHideFlags
	{
		HIDE_NONE = 0,
		HIDE_GLOVES = 1,
		HIDE_SUITSTORAGE = 2, // Not implemented
		HIDE_JUMPSUIT = 4,
		HIDE_SHOES = 8,
		HIDE_MASK = 16,
		HIDE_EARS = 32,
		HIDE_EYES = 64,
		HIDE_FACE = 128,
		HIDE_HAIR = 256,
		HIDE_FACIALHAIR = 512,
		HIDE_NECK = 1024,
		HIDE_ALL = ~0
	}
}
