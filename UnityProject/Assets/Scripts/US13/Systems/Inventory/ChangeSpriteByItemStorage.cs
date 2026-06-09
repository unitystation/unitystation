using System.Linq;
using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Managers.NetworkManagement;

namespace US13.Systems.Inventory
{
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
}
