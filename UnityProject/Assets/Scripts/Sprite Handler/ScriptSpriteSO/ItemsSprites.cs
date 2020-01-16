using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemsSprites", menuName = "ScriptableObjects/SpriteDataSO/ItemsSprites")]
public class ItemsSprites : SpriteDataSO
{
	public DataAndSpritesData LeftHand = new DataAndSpritesData();
	public DataAndSpritesData RightHand = new DataAndSpritesData();

	public DataAndSpritesData InventoryIcon = new DataAndSpritesData();

}
