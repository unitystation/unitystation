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
	public Color color = Color.white;

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
		//SetColor
		//spriteRenderer.color = value;
	}
	public void SetCustomisation(string Name, PlayerCustomisation type, BodyPartSpriteName part = BodyPartSpriteName.Null) { 
	}


	public void SetReference(GameObject Item)
	{
		UpdateReferenceOffset();
		//Logger.Log("Received!!" + this.name);
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
			GameObjectReference = Item;
			//Logger.Log("DD!!" + Item.name);
			//Logger.Log("is here!");
			if (spriteType == SpriteHandType.RightHand || spriteType == SpriteHandType.LeftHand)
			{
				var SHD = Item.GetComponent<ItemAttributes>()?.spriteHandlerData;
				if (SHD != null)
				{
					spriteHandler.Infos = SHD.Infos;
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
			}
			else {
				var clothing = Item.GetComponent<Clothing>();
				//Logger.Log("1");
				if (clothing != null)
				{
					//Logger.Log("2");
					clothing.Start(); //lagyy?
					spriteHandler.Infos = clothing.SpriteInfo;
					spriteHandler.ChangeSprite(clothing.ReturnState(ClothingVariantType.Default));
					spriteHandler.PushTexture();
				}
			}
		}
		UpdateReferenceOffset();
	}

	private void UpdateReferenceOffset()
	{
		//Logger.Log("UpdateReferenceOffset" + currentDirection);
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
		//Logger.Log("UpdateSprite");
		//Logger.Log(this.name);
		//Logger.Log("A");
		if (spriteHandler != null)
		{
			//Logger.Log("B");
			if (spriteHandler.Infos != null)
			{
				//Logger.Log("C");
				spriteHandler.ChangeSpriteVariant(referenceOffset);
			}

		}
	}
}