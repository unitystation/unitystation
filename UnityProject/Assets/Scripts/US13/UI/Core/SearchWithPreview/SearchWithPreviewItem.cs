using TMPro;
using UnityEngine;
using US13.Core.Sprite_Handler;

namespace US13.UI.Core.SearchWithPreview
{
	public class SearchWithPreviewItem : MonoBehaviour
	{
		public ISearchSpritePreview SetItem;
		public SearchWithPreview Master;

		public SpriteHandler spriteHandler;

		public TMP_Text Text;

		public void SetUp(ISearchSpritePreview Item,SearchWithPreview master )
		{
			Master = master;
			SetItem = Item;

			spriteHandler.SetSpriteSO(Item.Sprite);
			Text.text = Item.Name;
		}


		public void SetButton()
		{
			Master.OptionChosen(SetItem);
		}
	}
}
