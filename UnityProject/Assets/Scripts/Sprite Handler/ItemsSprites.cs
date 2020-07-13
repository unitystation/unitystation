using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemsSprites
{
	public SpriteSheetAndData LeftHand = new SpriteSheetAndData();
	public SpriteSheetAndData RightHand = new SpriteSheetAndData();
	public SpriteSheetAndData InventoryIcon = new SpriteSheetAndData();


	public SpriteDataSO SpriteInventoryIcon = new SpriteDataSO();
	public SpriteDataSO SpriteLeftHand = new SpriteDataSO();
	public SpriteDataSO SpriteRightHand = new SpriteDataSO();


	public List<Color> Palette = new List<Color>(new Color[8]);
	public bool IsPaletted = false;

}
