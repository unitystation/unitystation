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
	public Color color;

	public SpriteHandler spriteHandler;
	//public SpriteRenderer spriteRenderer;
	private Sprite[] sprites;

	public string spriteSheetName;

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
		//SetColor
		//spriteRenderer.color = value;
	}

	public void SetReference(int value, GameObject Item)
	{
		//Logger.Log("value" + value.ToString());
		reference = value;
		if (Item == null)
		{
			if (spriteHandler != null) //need to remove 
			{
				spriteHandler.Infos = null;
				spriteHandler.PushTexture();
			}
		}
		if (Item != null)
		{

			//Logger.Log("is here!");
			if (spriteType == SpriteHandType.RightHand || spriteType == SpriteHandType.LeftHand)
			{
				var itemAttributes = Item.GetComponent<ItemAttributes>().spriteHandlerData;
				spriteHandler.Infos = itemAttributes.Infos;
				if (spriteType == SpriteHandType.RightHand)
				{
					spriteHandler.ChangeSprite(1);
				}
				else
				{
					spriteHandler.ChangeSprite(0);
				}
				spriteHandler.PushTexture();
			}
			else {
				var clothing = Item.GetComponent<Clothing>();
				if (clothing != null)
				{
					spriteHandler.Infos = clothing.SpriteInfo;
					spriteHandler.ChangeSprite(clothing.ReturnState(ClothingVariantType.Default));
					spriteHandler.PushTexture();
				}
			}



		}

		//if (reference == -1)
		//{
		//	//UpdateSprite();
		//	return;
		//}

		//if (spriteType != SpriteHandType.Other)
		//{
		//	string networkRef = reference.ToString();
		//	int code = (int)char.GetNumericValue(networkRef[0]);
		//	switch (code)
		//	{
		//		case 1:
		//			spriteSheetName = "items_";
		//			break;
		//		case 2:
		//			spriteSheetName = "clothing_";
		//			break;
		//		case 3:
		//			spriteSheetName = "guns_";
		//			break;
		//	}

		//}

		//sprites = SpriteManager.PlayerSprites[spriteSheetName];

		//UpdateSprite();
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
		//Logger.Log(this.name);
		//Logger.Log("A");
		if (spriteHandler != null)
		{
			//Logger.Log("B");
			if (spriteHandler.Infos != null)
			{
				//Logger.Log("C");
				//spriteHandler.
				spriteHandler.ChangeSpriteVariant(referenceOffset);
			}

		}

		if (reference == -1)
		{
			//spriteRenderer.sprite = null;
			return;
		}

		int index = referenceOffset;
		if (spriteType != SpriteHandType.Other)
		{
			string networkRef = reference.ToString();
			networkRef = networkRef.Remove(0, 1);
			index += int.Parse(networkRef);
		}
		else
		{
			index += reference;
		}
		//spriteRenderer.sprite = sprites[index];
	}

}