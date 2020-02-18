using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemsSprites 
{
	public SpriteSheetAndData LeftHand = new SpriteSheetAndData();
	public SpriteSheetAndData RightHand = new SpriteSheetAndData();

	public SpriteSheetAndData InventoryIcon = new SpriteSheetAndData();

	public Color[] Palette = new Color[8];
	public bool IsPaletted = false;

}
