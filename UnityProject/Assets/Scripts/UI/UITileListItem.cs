using PlayGroup;
using Tilemaps.Scripts.Tiles;
using UnityEngine;
using UnityEngine.UI;

public class UITileListItem : MonoBehaviour
{
	public Button button;
	public Image image;

	private GameObject item;
	private SpriteRenderer itemSpriteRenderer;
	public Text text;
	private LayerTile tile;
	private Sprite tileSprite;

	public GameObject Item
	{
		get { return item; }
		set
		{
			item = value;
			tile = null;
			itemSpriteRenderer = value.transform.GetComponentInChildren<SpriteRenderer>(false);
			image.sprite = itemSpriteRenderer.sprite;
			tileSprite = null;
			text.text = value.name;
		}
	}

	public LayerTile Tile
	{
		get { return tile; }
		set
		{
			item = null;
			tile = value;
			itemSpriteRenderer = null;
			tileSprite = tile?.PreviewSprite;
			image.sprite = tileSprite;
			text.text = value?.name;
		}
	}

	public void Update()
	{
		//Anyone know of a better way to get the "sprite" in the UI to sync with the sprite in game?
		if (itemSpriteRenderer)
		{
			image.sprite = itemSpriteRenderer.sprite;
		}
	}

	public void Button_Item()
	{
		//clicking on tile does nothing
		if (tile && item == null)
		{
			return;
		}
		//Interact with item. In case we picked up the item, it should no longer be on the list
		if (PlayerManager.LocalPlayerScript.inputController.Interact(item.transform))
		{
			UITileList.RemoveTileListItem(transform.gameObject);
		}
		;
	}
}