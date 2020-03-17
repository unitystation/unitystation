using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SeedExtractorItemEntry : DynamicEntry
{
	[SerializeField]
	private Color regularColor = Color.gray;
	//[SerializeField]
	//private Color emptyStockColor = Color.red;
	private GameObject seedItem;
	private GUI_SeedExtractor seedExtractorWindow;
	[SerializeField]
	private NetLabel seedStats = null;
	//[SerializeField]
	//private NetLabel itemCount = null;
	[SerializeField]
	private NetPrefabImage itemIcon = null;
	[SerializeField]
	private NetColorChanger itemBackground = null;

	public void SetItem(GameObject item, GUI_SeedExtractor correspondingWindow)
	{
		seedItem = item;
		var seedPacket = seedItem.GetComponent<SeedPacket>();
		seedExtractorWindow = correspondingWindow;
		seedStats.SetValue = $"{seedPacket.plantData.Potency.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.Yield.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.GrowthSpeed.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.Endurance.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.Lifespan.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.WeedResistance.ToString().PadLeft(3)} " +
			$"{seedPacket.plantData.WeedGrowthRate.ToString().PadLeft(3)}";
		itemIcon.SetValue = seedItem.name;
		itemBackground.SetValue = ColorUtility.ToHtmlStringRGB(regularColor);
	}

	public void Dispense()
	{
		if (seedItem == null || seedExtractorWindow == null)
		{
			return;
		}
		seedExtractorWindow.DispenseSeedPacket(seedItem);
	}
}
