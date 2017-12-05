using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayGroup;

public class UITileListItem : MonoBehaviour
{

    public Button button;
    public Image image;
    public Text text;

    private GameObject tileItem;
    private SpriteRenderer spriteRenderer;

    public void Update()
    {
        //Anyone know of a better way to get the "sprite" in the UI to sync with the sprite in game?
        if (spriteRenderer)
        {
            image.sprite = spriteRenderer.sprite;
        }
    }

    public void Button_Item()
    {
        //Interact with item. In case we picked up the item, it should no longer be on the list
        if (PlayerManager.LocalPlayerScript.inputController.Interact(tileItem.transform))
        {
            UITileList.RemoveTileListItem(transform.gameObject);
        };
    }

    public GameObject TileItem
    {
        get { return tileItem; }
        set
        {
            tileItem = value;
            spriteRenderer = value.transform.GetComponentInChildren<SpriteRenderer>(false);
            image.sprite = spriteRenderer.sprite;
            text.text = value.name;
        }
    }
}
