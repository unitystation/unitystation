using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// The main controller for each component of its Sprites 
/// </summary>
public class SpriteHandlerController : NetworkBehaviour
{

	private Pickupable pickupable;
	private ItemAttributesV2 itemAttributes;


	/// <summary>
	/// This needs to be changed To support multiple
	/// </summary>
	[Tooltip("Fill this out if you dont want it automatically picking the spriteHandler for you ")]
	[SerializeField]
	private SpriteHandler spriteHandler;

	/// <summary>
	/// Fall-back for old Objects
	/// </summary>
	private SpriteRenderer spriteRenderer;

	private bool Initialised;

	private void Initialise() { 
		if (spriteHandler == null)
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}
		if (spriteHandler == null)
		{
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}
		itemAttributes = GetComponent<ItemAttributesV2>();
		pickupable = GetComponent<Pickupable>();
		Initialised = true;
		if (itemAttributes.ItemSprites.InventoryIcon.Data.HasSprite())
		{
			SetIcon(itemAttributes.ItemSprites);
		}
	}

	private void Awake()
	{
		Initialise();
	}

	/// <summary>
	/// Sets all the related sprites for items Currently supports only one item sprite
	/// </summary>
	public void SetSprites(ItemsSprites newSprites)
	{
		if (!Initialised){
			Initialise();
		}
		itemAttributes.SetSprites(newSprites);
		pickupable.SetPlayerItemsSprites(newSprites);
		SetIcon(newSprites);
		pickupable.RefreshUISlotImage();

	}

	private void SetIcon(ItemsSprites newSprites) {
		
		if (!Initialised)
		{
			Initialise();
		}
		if (spriteHandler != null)
		{
			newSprites.InventoryIcon.Data.palettes = new List<List<Color>>(){ new List<Color>(newSprites.Palette) };
			newSprites.InventoryIcon.Data.isPaletteds = new List<bool>() { newSprites.IsPaletted };

			spriteHandler.SetInfo(newSprites.InventoryIcon.Data);
		}
		else if (spriteRenderer != null){
			spriteRenderer.sprite = newSprites.InventoryIcon.Data.ReturnFirstSprite();
		}
	}


	public void SetPaletteOfCurrentSprite(List<Color> newPalette)
	{
		pickupable.RefreshUISlotImage();
		spriteHandler.SetPaletteOfCurrentSprite(newPalette);
	}
}
