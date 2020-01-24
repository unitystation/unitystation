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


	public ItemsSprites  InHandsSprites => inHandsSprites;

	[Tooltip("The In hands Sprites If it has any")]
	[SerializeField]
	private ItemsSprites inHandsSprites;


	/// <summary>
	/// This needs to be changed To support multiple
	/// </summary>
	[Tooltip("Fill this out if you dont want it automatically picking the spriteHandler for you ")]
	[SerializeField]
	private SpriteHandler spriteHandler;

	/// <summary>
	/// Full-back for old components
	/// </summary>
	private SpriteRenderer spriteRenderer;

	private bool Initialised;

	private void Start()
	{		if (spriteHandler == null)
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}
		if (spriteHandler == null) { 
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		}
		itemAttributes = GetComponent<ItemAttributesV2>();
		pickupable = GetComponent<Pickupable>();
		Initialised = true;
	}

	/// <summary>
	/// Sets all the related sprites for items Currently supports only one item sprite
	/// </summary>
	public void SetSprites(ItemsSprites newSprites)
	{
		if (!Initialised){
			Start();
		}
		inHandsSprites = newSprites;
		pickupable.SetPlayerItemsSprites(newSprites);
		SetIcon(newSprites.InventoryIcon.Data);
		pickupable.RefreshUISlotImage();

	}

	private void SetIcon(SpriteData spriteData) {		if (!Initialised)
		{
			Start();
		}
		if (spriteHandler != null)
		{
			spriteHandler.SetInfo(spriteData);
		}
		else if (spriteRenderer != null){
			spriteRenderer.sprite = spriteData.ReturnFirstSprite();
		}
	}
}
