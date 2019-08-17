using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ClothingData", menuName = "ScriptableObjects_ClothingData", order = 1)]
public class ClothingData : ScriptableObject
{
	public GameObject PrefabVariant;

	public EquippedData Base; //Your Basic clothing, If any missing data on any of the other points it will take it from here
	public EquippedData Base_Adjusted; //Variant for if it is Worn differently
	public EquippedData DressVariant; //humm yeah Dresses 
	public List<EquippedData> Variants; //For when you have 1 million colour variants


    public void OnValidate()
	{
		setEquippedData(Base);
		setEquippedData(Base_Adjusted);
		setEquippedData(DressVariant);
		foreach (var Variant in Variants) { 
			setEquippedData(Variant);
		}
	}

	public void setEquippedData(EquippedData equippedData)
	{
		equippedData.Equipped.Equipped.setSprites();
		equippedData.InHandsLeft.Equipped.setSprites();
		equippedData.InHandsRight.Equipped.setSprites();
	}

}

[System.Serializable]
public class TextureAndData
{
	public SpriteSheet Equipped;
	public TextAsset EquippedData;

}

[System.Serializable]
public class EquippedData
{
	public TextureAndData Equipped;
	public TextureAndData InHandsLeft;
	public TextureAndData InHandsRight;
	public Sprite ItemIcon;
}