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
	private SpriteHandlerController spriteHandlerController;
	//TODO: This can probably be migrated to this component rather than using a separate SO, since
	//there's probably no situation where we'd want to re-use the same cloth data on more than one item.
	[Tooltip("Clothing data describing the various sprites for this clothing.")]
	[SerializeField]
	private BaseClothData clothData = null;

	[Tooltip("Index of the variant to use. -1 for default.")]
	[SerializeField]
	private int variantIndex = -1;

	[Tooltip("Type of variant to use of the clothing data.")]
	[SerializeField][SyncVar(hook = nameof(SyncSprites))]
	private ClothingVariantType variantType = ClothingVariantType.Default;

	[Tooltip("Determine when a piece of clothing hides another")]
	[SerializeField, EnumFlag]
	private ClothingHideFlags hideClothingFlags = ClothingHideFlags.HIDE_NONE;

	private ItemAttributesV2 myItem;
	private Pickupable myPickupable;

	private Dictionary<ClothingVariantType, int> variantStore = new Dictionary<ClothingVariantType, int>();
	private List<int> variantList;
	private SpriteData spriteInfo;

	/// <summary>
	/// Clothing item this is currently equipped to, if there is one. Will be updated when the data is synced.
	/// </summary>
	private ClothingItem clothingItem;

	/// <summary>
	/// The sprite info for this clothing. Describes all the sprites it has for various situations.
	/// </summary>
	public SpriteData SpriteInfo => spriteInfo;

	//TODO: Refactor this, quite confusing
	/// <summary>
	/// The index of the sprites that should currently be used for rendering this, an index into SpriteData.List
	/// </summary>
	public int SpriteInfoState => variantStore.ContainsKey(variantType) ? variantStore[variantType] : 0;

	/// <summary>
	/// Determine when a piece of clothing hides another
	/// </summary>
	public ClothingHideFlags HideClothingFlags => hideClothingFlags;

	private SpriteHandler inventoryIcon;

	private void Awake()
	{
		spriteHandlerController = GetComponent<SpriteHandlerController>();
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
				spriteInfo = SetUpSheetForClothingData(clothingData);
				SetUpFromClothingData(clothingData.Base);

				switch (variantType)
				{
					case ClothingVariantType.Default:
						if (variantIndex > -1)
						{
							if (!(clothingData.Variants.Count >= variantIndex))
							{
								SetUpFromClothingData(clothingData.Variants[variantIndex]);
							}
						}
						break;
					case ClothingVariantType.Skirt:
						SetUpFromClothingData(clothingData.DressVariant);
						break;
					case ClothingVariantType.Tucked:
						SetUpFromClothingData(clothingData.Base_Adjusted);
						break;
				}
				break;

			case ContainerData containerData:
				this.spriteInfo = SpriteFunctions.SetupSingleSprite(containerData.Sprites.Equipped);
				SetUpFromClothingData(containerData.Sprites);
				break;

			case BeltData beltData:
				this.spriteInfo = SpriteFunctions.SetupSingleSprite(beltData.sprites.Equipped);
				SetUpFromClothingData(beltData.sprites);
				break;

			case HeadsetData headsetData:
			{
				var Headset = GetComponent<Headset>();
				this.spriteInfo = SpriteFunctions.SetupSingleSprite(headsetData.Sprites.Equipped);
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
		SpriteSOData.LeftHand = (equippedData.InHandsLeft);
		SpriteSOData.RightHand = (equippedData.InHandsRight);
		SpriteSOData.InventoryIcon = (equippedData.ItemIcon);
		SpriteSOData.IsPaletted = equippedData.IsPaletted;

		spriteHandlerController.SetSprites(SpriteSOData);
	}


	public void AssignPaletteToSprites(List<Color> palette)
	{
		if (myItem.ItemSprites.IsPaletted)
		{
			spriteHandlerController.SetPaletteOfCurrentSprite(palette);
			clothingItem?.spriteHandler.SetPaletteOfCurrentSprite(palette);
			myPickupable.SetPalette(palette);
		}
	}


	private SpriteData SetUpSheetForClothingData(ClothingData clothingData)
	{
		var SpriteInfos = new SpriteData();

		SpriteInfos.List = new List<List<List<SpriteHandler.SpriteInfo>>>();
		SpriteInfos.isPaletteds = new List<bool>();
		SpriteInfos.palettes = new List<List<Color>>();
		int c = 0;

		SpriteInfos.List.Add(SpriteFunctions.CompleteSpriteSetup(clothingData.Base.Equipped));
		SpriteInfos.isPaletteds.Add(clothingData.Base.IsPaletted);
		SpriteInfos.palettes.Add(new List<Color>(clothingData.Base.Palette));

		variantStore[ClothingVariantType.Default] = c;
		c++;

		if (clothingData.Base_Adjusted.Equipped.Texture != null)
		{
			SpriteInfos.List.Add(SpriteFunctions.CompleteSpriteSetup(clothingData.Base_Adjusted.Equipped));
			SpriteInfos.isPaletteds.Add(clothingData.Base_Adjusted.IsPaletted);
			SpriteInfos.palettes.Add(new List<Color>(clothingData.Base_Adjusted.Palette));
			variantStore[ClothingVariantType.Tucked] = c;
			c++;
		}

		if (clothingData.DressVariant.Equipped.Texture != null)
		{
			SpriteInfos.List.Add(SpriteFunctions.CompleteSpriteSetup(clothingData.DressVariant.Equipped));
			SpriteInfos.isPaletteds.Add(clothingData.DressVariant.IsPaletted);
			SpriteInfos.palettes.Add(new List<Color>(clothingData.DressVariant.Palette));
			variantStore[ClothingVariantType.Skirt] = c;
			c++;
		}
		if (clothingData.Variants.Count > 0)
		{
			foreach (var Variant in clothingData.Variants)
			{
				SpriteInfos.List.Add(SpriteFunctions.CompleteSpriteSetup(Variant.Equipped));
				SpriteInfos.isPaletteds.Add(Variant.IsPaletted);
				SpriteInfos.palettes.Add(new List<Color>(Variant.Palette));
				variantStore[ClothingVariantType.Skirt] = c; // Doesn't make sense
				c++;
			}
		}

		return (SpriteInfos);
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
		return clothData == null ? null : clothData.GetPaletteOrNull(variantIndex);
	}

	public enum ClothingVariantType
	{
		Default = 0,
		Tucked = 1,
		Skirt = 2
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