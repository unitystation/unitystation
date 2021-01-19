using UnityEngine;
using System.Collections.Generic;
using Items;

public enum SpriteHandType
{
	Other,
	RightHand,
	LeftHand
}

public delegate void OnClothingEquippedDelegate(ClothingV2 clothing, bool isEquipped);

/// <summary>
/// For the Individual clothing player sprite renderers
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ClothingItem : MonoBehaviour
{
	[Tooltip("Slot this clothing item is equipped to.")]
	public NamedSlot Slot;

	/// <summary>
	/// Absolute orientation
	/// </summary>
	private Orientation currentDirection = Orientation.Down;

	protected int referenceOffset;

	public SpriteHandler spriteHandler;

	public GameObject GameObjectReference;

	//choice between left or right or other(clothing)
	public SpriteHandType spriteType;

	public PlayerScript thisPlayerScript;

	/// <summary>
	/// Player equipped or unequipped some clothing from ClothingItem slot
	/// </summary>
	public event OnClothingEquippedDelegate OnClothingEquipped;

	/// <summary>
	/// Direction clothing is facing (absolute)
	/// </summary>
	public Orientation Direction
	{
		set
		{
			currentDirection = value;
			UpdateReferenceOffset();
		}
		get { return currentDirection; }
	}

	/// <summary>
	/// Are we holding item or clothing in hands?
	/// </summary>
	private bool InHands
	{
		get { return spriteType == SpriteHandType.RightHand || spriteType == SpriteHandType.LeftHand; }
	}

	private void Awake()
	{
		UpdateSprite();
	}

	public void SetColor(Color value)
	{
		if (spriteHandler != null)
		{
			spriteHandler.SetColor(value);
		}
	}

	public virtual void SetReference(GameObject item)
	{
		UpdateReferenceOffset();

		if (item == null)
		{
			RemoveItemFromEquipment();
		}
		else
		{
			AddItemToEquipment(item);
		}

		UpdateReferenceOffset();
	}

	private void RemoveItemFromEquipment()
	{
		if (spriteHandler != null)
		{
			spriteHandler.Empty();
		}

		if (!InHands && GameObjectReference != null)
		{
			// did we take off clothing?
			var unequippedClothing = GameObjectReference.GetComponent<ClothingV2>();

			if (unequippedClothing == null)
			{
				//Not clothing, maybe PDA
				return;
			}

			// Unhide the players's slots defined in the clothing's HiddenSlots, as we're removing it.
			thisPlayerScript.Equipment.obscuredSlots &= ~unequippedClothing.HiddenSlots;

			if (unequippedClothing)
				OnClothingEquipped?.Invoke(unequippedClothing, false);
		}

		GameObjectReference = null;
	}

	private void AddItemToEquipment(GameObject item)
	{
		GameObjectReference = item;

		if (InHands)
		{
			var itemAttributesV2 = item.GetComponent<ItemAttributesV2>();
			var inHandsSprites = itemAttributesV2.ItemSprites;

			if (inHandsSprites != null)
			{
				SetInHand(inHandsSprites);
			}
		}
		else
		{
			var equippedClothing = item.GetComponent<ClothingV2>();

			if (equippedClothing == null)
			{
				//Not clothing, maybe PDA
				return;
			}

			equippedClothing.LinkClothingItem(this);

			// Set the slots defined in hidesSlots as hidden.
			thisPlayerScript.Equipment.obscuredSlots |= equippedClothing.HiddenSlots;

			// Some items like trash bags / mining satchels can be equipped but are not clothing and do not show on character sprite
			// But for the others, we call the OnClothingEquipped event.

			// call the event of equiped clothing
			OnClothingEquipped?.Invoke(equippedClothing, true);
		}
	}

	public void RefreshFromClothing(ClothingV2 clothing)
	{
		spriteHandler.SetCatalogue(clothing.SpriteDataSO);
		spriteHandler.ChangeSprite(clothing.SpriteInfoState);
		List<Color> palette = clothing.GetComponent<ItemAttributesV2>()?.ItemSprites?.Palette;
		if (palette != null)
		{
			spriteHandler.SetPaletteOfCurrentSprite(palette,  Network:false);
		}


		PushTexture();
	}

	private void UpdateReferenceOffset()
	{
		if (currentDirection == Orientation.Down)
		{
			referenceOffset = 0;
		}

		if (currentDirection == Orientation.Up)
		{
			referenceOffset = 1;
		}

		if (currentDirection == Orientation.Right)
		{
			referenceOffset = 2;
		}

		if (currentDirection == Orientation.Left)
		{
			referenceOffset = 3;
		}

		UpdateSprite();
	}

	public virtual void UpdateSprite()
	{
		if (spriteHandler != null)
		{
			spriteHandler.ChangeSpriteVariant(referenceOffset, false);
		}
	}

	public void PushTexture()
	{
		if (spriteHandler != null)
		{
			spriteHandler.PushTexture(false);
		}
	}

	public void SetInHand(ItemsSprites _ItemsSprites)
	{
		if (_ItemsSprites != null)
		{
			if (spriteType == SpriteHandType.RightHand)
			{
				spriteHandler.SetSpriteSO(_ItemsSprites.SpriteRightHand,Network: false);
			}
			else
			{
				spriteHandler.SetSpriteSO(_ItemsSprites.SpriteLeftHand, Network:false);
			}

			spriteHandler.SetPaletteOfCurrentSprite(_ItemsSprites.Palette,  Network:false);
		}
	}
}
