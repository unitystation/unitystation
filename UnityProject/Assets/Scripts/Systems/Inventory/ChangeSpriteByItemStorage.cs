using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChangeSpriteByItemStorage : MonoBehaviour
{

	public ItemStorage Storage;

	private ItemSlot ItemSlot;

	public SpriteHandler Preview;

	public SpriteHandler BagsSprite;

    void Start()
    {
	    if (CustomNetworkManager.IsServer == false) return;
	    Storage = this.GetComponent<ItemStorage>();
	    ItemSlot = Storage.GetItemSlots().First();
	    ItemSlot.OnSlotContentsChangeServer.AddListener(UpdateSprite);
    }

    public void UpdateSprite()
    {
	    if (ItemSlot.Item == null)
	    {
		    Preview.SetCatalogueIndexSprite(0);
		    if (BagsSprite != null) BagsSprite.SetCatalogueIndexSprite(0);
	    }
	    else
	    {
		    Preview.PushTexture();
		    Preview.SetSpriteSO(ItemSlot.Item.GetComponentInChildren<SpriteHandler>().PresentSpritesSet);
		    if (BagsSprite != null) BagsSprite.SetCatalogueIndexSprite(1);
	    }
    }
}
