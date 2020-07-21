using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Indicates that a given item functions as an article of clothing. Contains the data related to the clothing / how it should look.
/// New and improved, removes need for UniCloth type stuff, works
/// well with using prefab variants.
/// </summary>
public class ClothingV2 : NetworkBehaviour
{
	//TODO: This can probably be migrated to this component rather than using a separate SO, since
	//there's probably no situation where we'd want to re-use the same cloth data on more than one item.
	[Tooltip("Clothing data describing the various sprites for this clothing.")]
	[SerializeField]
	private BaseClothData clothData = null;

	[Tooltip("Type of variant to use of the clothing data.")]
	[SerializeField][SyncVar(hook = nameof(SyncSprites))]
	private ClothingVariantType variantType = ClothingVariantType.Default;

	[Tooltip("Determine when a piece of clothing hides another")]
	[SerializeField, EnumFlag]
	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;

	private ItemAttributesV2 myItem;
	private Pickupable myPickupable;

	public List<SpriteDataSO> SpriteDataSO = new List<SpriteDataSO>();
	private bool isAdjusted;
	/// <summary>
	/// Clothing item this is currently equipped to, if there is one. Will be updated when the data is synced.
	/// </summary>
	private ClothingItem clothingItem;

	/// <summary>
	/// The sprite info for this clothing. Describes all the sprites it has for various situations.
	/// </summary>
	//public SpriteData SpriteInfo => spriteInfo;

	//TODO: Refactor this, quite confusing
	/// <summary>
	/// The index of the sprites that should currently be used for rendering this, an index into SpriteData.List
	/// </summary>
	public int SpriteInfoState => isAdjusted ? 1 : 0;

	/// <summary>
	/// Determine when a piece of clothing hides another
	/// </summary>
	public ClothingHideFlags HideClothingFlags => hideClothingFlags;

	private SpriteHandler inventoryIcon;

	private void Awake()
	{
		myItem = GetComponent<ItemAttributesV2>();
		myPickupable = GetComponent<Pickupable>();
		TryInit();
	}

	//set up the sprites / config of this instance using the cloth data
	private void TryInit()
	{
		switch (clothData)
		{
			case ClothingData clothingData:
				SpriteDataSO.Add(clothingData.Base.SpriteEquipped);
				SpriteDataSO.Add(clothingData.Base_Adjusted.SpriteEquipped);
				SetUpFromClothingData(clothingData.Base);
				break;

			case ContainerData containerData:
				SpriteDataSO.Add(containerData.Sprites.SpriteEquipped);
				SetUpFromClothingData(containerData.Sprites);
				break;

			case BeltData beltData:
				SpriteDataSO.Add(beltData.sprites.SpriteEquipped);
				SetUpFromClothingData(beltData.sprites);
				break;

			case HeadsetData headsetData:
			{
				var Headset = GetComponent<Headset>();
				SpriteDataSO.Add(headsetData.Sprites.SpriteEquipped);
				SetUpFromClothingData(headsetData.Sprites);
				Headset.EncryptionKey = headsetData.Key.EncryptionKey;
				break;
			}
		}
	}

	private void SetUpFromClothingData(EquippedData equippedData)
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
			clothingItem?.spriteHandler.SetPaletteOfCurrentSprite(palette,  Network:false);
			myPickupable.SetPalette(palette);
		}
	}


	public void LinkClothingItem(ClothingItem clothingItem)
	{
		this.clothingItem = clothingItem;
		RefreshClothingItem();
	}

	private void SyncSprites(ClothingVariantType oldValue, ClothingVariantType newValue)
	{
		variantType = newValue;
		TryInit();
		RefreshClothingItem();
	}

	[Server]
	public void ServerChangeVariant(ClothingVariantType variant)
	{
		variantType = variant;
	}

	private void RefreshClothingItem()
	{
		if (clothingItem != null)
		{
			clothingItem.RefreshFromClothing(this);
		}
	}

	public List<Color> GetPaletteOrNull()
	{
		return clothData == null ? null : clothData.GetPaletteOrNull(0);
	}

	public enum ClothingVariantType
	{
		Default = 0,
		Tucked = 1,
	}
}

/// <summary>
/// Bit flags which determine when a piece of clothing hides another.
/// IE a helmet hiding glasses.
/// </summary>
[Flags]
public enum ClothingHideFlags
{
	HIDE_NONE = 0,
	HIDE_GLOVES =  1,
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