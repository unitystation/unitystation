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

	public override List<Color> GetPaletteOrNull(int variantIndex)
	{
		// TODO: Get alternate palette if necessary.
		//if (variantIndex == -1)
		//{
		return Base.IsPaletted ? new List<Color>(Base.Palette) : null;
		//}

		//return null;
	}


}

[System.Serializable]
public class EquippedData
{
	public SpriteDataSO SpriteEquipped;
	public SpriteDataSO SpriteInHandsLeft;
	public SpriteDataSO SpriteInHandsRight;
	public SpriteDataSO SpriteItemIcon;
	public Color[] Palette = new Color[8];
	public bool IsPaletted = false;

	public void Combine(EquippedData parent)
	{
		if (SpriteEquipped != null)
		{
			SpriteEquipped = parent.SpriteEquipped;
		}

		if (SpriteInHandsLeft != null)
		{
			SpriteInHandsLeft = parent.SpriteInHandsLeft;
		}

		if (SpriteInHandsRight != null)
		{
			SpriteInHandsRight = parent.SpriteInHandsRight;
		}

		if (SpriteItemIcon != null)
		{
			SpriteItemIcon = parent.SpriteItemIcon;
		}
	}
}