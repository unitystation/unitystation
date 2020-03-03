using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemsSprites 
{
	public SpriteSheetAndData LeftHand = new SpriteSheetAndData();
	public SpriteSheetAndData RightHand = new SpriteSheetAndData();

	public SpriteSheetAndData InventoryIcon = new SpriteSheetAndData();

	public List<Color> Palette = new List<Color>(new Color[8]);
	public bool IsPaletted = false;

}
