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
			text.text = value.ExpensiveName();
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
			text.text = value?.DisplayName;
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void UpdateMe()
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
	}
}
