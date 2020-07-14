using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemsSprites
{
	public SpriteDataSO SpriteInventoryIcon = null;
	public SpriteDataSO SpriteLeftHand = null;
	public SpriteDataSO SpriteRightHand = null;

	public List<Color> Palette = new List<Color>(new Color[8]);
	public bool IsPaletted = false;

}
