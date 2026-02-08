using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Items;
using Util;

namespace US13.Systems.Inventory
{
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
}
