using System.Collections;
using System.Collections.Generic;
using TMPro;
using UISearchWithPreview;
using UnityEngine;

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
