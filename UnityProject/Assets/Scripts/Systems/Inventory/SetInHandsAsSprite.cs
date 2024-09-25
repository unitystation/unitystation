using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

public class SetInHandsAsSprite : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
	    var IA2 = this.GetComponentCustom<ItemAttributesV2>();
	   var Sprite = this.GetComponentInChildren<SpriteHandler>();
	   Sprite.OnSpriteDataSOChanged += UpdateSprites;

	   IA2.ItemSprites.SpriteLeftHand =  Sprite.PresentSpritesSet;
	   IA2.ItemSprites.SpriteRightHand =  Sprite.PresentSpritesSet;
    }

    public void UpdateSprites(SpriteDataSO SpriteDataSO)
    {
	    var IA2 = this.GetComponentCustom<ItemAttributesV2>();
	    IA2.ItemSprites.SpriteLeftHand = SpriteDataSO;
	    IA2.ItemSprites.SpriteRightHand = SpriteDataSO;
    }

}
