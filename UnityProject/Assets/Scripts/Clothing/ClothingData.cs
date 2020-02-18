using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine;
using System;

//[InitializeOnLoad]
[CreateAssetMenu(fileName = "ClothingData", menuName = "ScriptableObjects/ClothingData", order = 1)]
public class ClothingData : BaseClothData
{
	public EquippedData Base; //Your Basic clothing, If any missing data on any of the other points it will take it from here
	public EquippedData Base_Adjusted; //Variant for if it is Worn differently
	public EquippedData DressVariant; //humm yeah Dresses
	public List<EquippedData> Variants; //For when you have 1 million colour variants

	public static void getClothingDatas(List<ClothingData> DataPCD)
	{
		DataPCD.Clear();
		var PCD = Resources.LoadAll<ClothingData>("textures/clothing");
		foreach (var PCDObj in PCD)
		{
			DataPCD.Add(PCDObj);
		}
	}

	private static void loadFolder(string folderpath, List<ClothingData> DataPCD)
	{
		folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
		foreach (var PCDObj in Resources.LoadAll<ClothingData>(folderpath))
		{
			if (!DataPCD.Contains(PCDObj))
			{
				DataPCD.Add(PCDObj);
			}
		}
	}

	public override string ToString()
	{
		return $"{name}";
	}


}

[System.Serializable]
public class EquippedData
{
	public SpriteSheetAndData Equipped;
	public SpriteSheetAndData InHandsLeft;
	public SpriteSheetAndData InHandsRight;
	public SpriteSheetAndData ItemIcon;
	public Color[] Palette = new Color[8];
	public bool IsPaletted = false;

	public void Combine(EquippedData parent)
	{
		if (Equipped.Texture == null)
		{
			Equipped = parent.Equipped;
		}

		if (InHandsLeft.Texture == null)
		{
			InHandsLeft = parent.InHandsLeft;
		}

		if (InHandsRight.Texture == null)
		{
			InHandsRight = parent.InHandsRight;
		}

		if (ItemIcon.Texture == null)
		{
			ItemIcon = parent.ItemIcon;
		}
	}
}