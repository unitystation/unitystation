using UnityEngine;

public enum SpriteHandType
{
	Other,
	RightHand,
	LeftHand
}

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

	public int reference;
	protected int referenceOffset;
	public Color color = Color.white;
	private int variantIndex = 0;

	public SpriteHandler spriteHandler;

	//public SpriteRenderer spriteRenderer;
	private Sprite[] sprites;

	public string spriteSheetName;

	public GameObject GameObjectReference;

	//choice between left or right or other(clothing)
	public SpriteHandType spriteType;

	public PlayerScript thisPlayerScript;

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

	private void Awake()
	{
		sprites = SpriteManager.PlayerSprites[spriteSheetName];
		UpdateSprite();
	}


	public void SetColor(Color value)
	{
		color = value;
		if (spriteHandler != null)
		{
			spriteHandler.SetColor(value);
		}
	}

	public virtual void SetReference(GameObject Item)
	{
		UpdateReferenceOffset();
		if (Item == null)
		{
			if (spriteHandler != null) //need to remove
			{
				spriteHandler.Infos = null;
				PushTexture();
			}
		}

		if (Item != null)
		{
			GameObjectReference = Item;
			if (spriteType == SpriteHandType.RightHand || spriteType == SpriteHandType.LeftHand)
			{
				var SpriteHandlerController = Item.GetComponent<SpriteHandlerController>();
				var InHandsSprites = SpriteHandlerController?.InHandsSprites;
				SetInHand(InHandsSprites);
			}
			else
			{
				var equippedClothing = Item.GetComponent<ClothingV2>();
				equippedClothing?.LinkClothingItem(this);
			}
		}

		UpdateReferenceOffset();
	}

	public void RefreshFromClothing(ClothingV2 clothing)
	{
		spriteHandler.Infos = clothing.SpriteInfo;
		spriteHandler.ChangeSprite(clothing.SpriteInfoState);
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
			if (spriteHandler.Infos != null)
			{
				spriteHandler.ChangeSpriteVariant(referenceOffset);
			}
		}
	}

	public void PushTexture()
	{
		if (spriteHandler != null)
		{
			spriteHandler.PushTexture();
		}
	}


	public void SetInHand(ItemsSprites _ItemsSprites) { 
		if (_ItemsSprites != null)
		{
			if (spriteType == SpriteHandType.RightHand)
			{
				spriteHandler.Infos = _ItemsSprites.RightHand.Data;
			}
			else
			{
				spriteHandler.Infos = _ItemsSprites.LeftHand.Data;
			}

			PushTexture();
		}
	
	}

}