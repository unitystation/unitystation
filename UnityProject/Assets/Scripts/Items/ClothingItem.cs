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
	private int referenceOffset;
	public Color color = Color.white;
	private int variantIndex = 0;
	private SpriteDataHandler SHD;

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

	public void SetReference(GameObject Item)
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
				var equippedAttributes = Item.GetComponent<IItemAttributes>();
				SHD = equippedAttributes?.SpriteDataHandler;
				if (SHD != null)
				{
					spriteHandler.Infos = SHD.Infos;
					if (spriteType == SpriteHandType.RightHand)
					{
						spriteHandler.ChangeSprite((spriteHandler.Infos.VariantIndex * 2) + 1);
					}
					else
					{
						spriteHandler.ChangeSprite(spriteHandler.Infos.VariantIndex * 2);
					}

					PushTexture();
				}
			}
			else
			{
				var equippedClothing = Item.GetComponent<IClothing>();
				equippedClothing?.LinkClothingItem(this);
			}
		}

		UpdateReferenceOffset();
	}

	public void RefreshFromClothing(IClothing clothing)
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

	public void UpdateSprite()
	{
		if (spriteHandler != null)
		{
			if (spriteHandler.Infos != null)
			{
				if (SHD)
				{
					if (spriteType == SpriteHandType.RightHand)
					{
						spriteHandler.ChangeSprite((SHD.Infos.VariantIndex * 2) + 1);
					}
					else
					{
						spriteHandler.ChangeSprite(SHD.Infos.VariantIndex * 2);
					}
				}
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
}