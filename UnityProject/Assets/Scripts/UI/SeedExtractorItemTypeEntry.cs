using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SeedExtractorItemTypeEntry : DynamicEntry
{
	[SerializeField]
	private Color regularColor = Color.gray;
	//[SerializeField]
	//private Color emptyStockColor = Color.red;
	private List<SeedPacket> seedPackets;
	[SerializeField]
	private EmptyItemList itemList = null;
	[SerializeField]
	private GUI_SeedExtractor seedExtractorWindow;
	[SerializeField]
	private NetLabel itemName = null;
	[SerializeField]
	private NetLabel itemCount = null;
	[SerializeField]
	private NetPrefabImage itemIcon = null;
	[SerializeField]
	private NetColorChanger itemBackground = null;

	public void SetItem(List<SeedPacket> item, GUI_SeedExtractor correspondingWindow)
	{
		seedPackets = item;
		seedExtractorWindow = correspondingWindow;
		itemName.SetValue = seedPackets.First().name;
		itemIcon.SetValue = seedPackets.First().name;
		itemCount.SetValue = $"({seedPackets.Count.ToString()})";
		itemBackground.SetValue = ColorUtility.ToHtmlStringRGB(regularColor);
	}

	public void Show()
	{
		seedExtractorWindow.SelectSeedType(seedPackets.First().name);
	}
}
