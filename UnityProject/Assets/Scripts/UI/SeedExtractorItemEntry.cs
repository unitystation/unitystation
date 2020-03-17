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
	private GameObject seedPacket;
	private GUI_SeedExtractor seedExtractorWindow;
	[SerializeField]
	private NetLabel itemName = null;
	//[SerializeField]
	//private NetLabel itemCount = null;
	[SerializeField]
	private NetPrefabImage itemIcon = null;
	[SerializeField]
	private NetColorChanger itemBackground = null;

	public void SetItem(GameObject item, GUI_SeedExtractor correspondingWindow)
	{
		seedPacket = item;
		seedExtractorWindow = correspondingWindow;
		itemName.SetValue = seedPacket.name;
		itemIcon.SetValue = seedPacket.name;
		itemBackground.SetValue = ColorUtility.ToHtmlStringRGB(regularColor);
	}

	public void Dispense()
	{
		if (seedPacket == null || seedExtractorWindow == null)
		{
			return;
		}
		seedExtractorWindow.DispenseSeedPacket(seedPacket);
	}
}
