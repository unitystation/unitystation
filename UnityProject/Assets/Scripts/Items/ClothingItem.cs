using UnityEngine;

public enum SpriteHandType
{
	Other,
	RightHand,
	LeftHand
}

[RequireComponent(typeof(SpriteRenderer))]
public class ClothingItem : MonoBehaviour
{
	/// <summary>
	/// Absolute orientation
	/// </summary>
	private Orientation currentDirection = Orientation.Down;
	public int reference = -1;
	private int referenceOffset;

	public SpriteRenderer spriteRenderer;
	private Sprite[] sprites;

	public string spriteSheetName;

	//choice between left or right or other(clothing)
	public SpriteHandType spriteType;

	public PlayerScript thisPlayerScript;

	public int Reference
	{
		set
		{
			reference = value;
			SetSprite();
		}
		get { return reference; }
	}

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

	private void Start()
	{
		sprites = SpriteManager.PlayerSprites[spriteSheetName];
		UpdateSprite();
	}

	public void Clear()
	{
		Reference = -1;
	}

	private void SetSprite(bool force = false)
	{
		if (reference == -1)
		{
			UpdateSprite();
			return;
		}

		if (spriteType == SpriteHandType.Other)
		{
			reference = Reference;
		}
		else
		{
			string networkRef = Reference.ToString();
			int code = (int)char.GetNumericValue(networkRef[0]);
			networkRef = networkRef.Remove(0, 1);
			int _reference = int.Parse(networkRef);
			switch (code)
			{
				case 1:
					spriteSheetName = "items_";
					break;
				case 2:
					spriteSheetName = "clothing_";
					break;
				case 3:
					spriteSheetName = "guns_";
					break;
			}
			if (spriteType == SpriteHandType.RightHand)
			{
				spriteSheetName = spriteSheetName + "righthand";
				reference = _reference;
			}
			else
			{
				spriteSheetName = spriteSheetName + "lefthand";
				reference = _reference;
			}
		}

		sprites = SpriteManager.PlayerSprites[spriteSheetName];
		UpdateSprite();
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
		if (spriteRenderer != null)
		{
			if (reference >= 0)
			{
				//If reference -1 then clear the sprite
				if (sprites != null)
				{
					int index = reference + referenceOffset;
					if (index < sprites.Length)
					{
						spriteRenderer.sprite = sprites[reference + referenceOffset];
					}
					else
					{
						Logger.LogTrace("Index is out of range for the reference sprite! ref: " + reference, Category.PlayerSprites);
					}
				}
			}
			else
			{
				spriteRenderer.sprite = null;
			}
		}
	}
}