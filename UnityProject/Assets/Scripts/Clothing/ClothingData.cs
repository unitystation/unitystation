using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "ClothingData", menuName = "ScriptableObjects/ClothingData", order = 1)]
public class ClothingData : ScriptableObject
{
	public GameObject PrefabVariant;

	public EquippedData Base; //Your Basic clothing, If any missing data on any of the other points it will take it from here
	public EquippedData Base_Adjusted; //Variant for if it is Worn differently
	public EquippedData DressVariant; //humm yeah Dresses 
	public List<EquippedData> Variants; //For when you have 1 million colour variants

	public ItemAttributesData ItemAttributes;

}



[System.Serializable]
public class EquippedData
{
	public SpriteSheetAndData Equipped;
	public SpriteSheetAndData InHandsLeft;
	public SpriteSheetAndData InHandsRight;
	public SpriteSheetAndData ItemIcon;
}