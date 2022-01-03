using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Items.Botany;

namespace UI.Objects.Botany
{
	public class SeedExtractorItemTypeEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;
		//[SerializeField]
		//private Color emptyStockColor = Color.red;
		private List<SeedPacket> seedPackets;
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
			itemName.SetValueServer(seedPackets.First().name);
			itemIcon.SetValueServer(seedPackets.First().name);
			itemCount.SetValueServer($"({seedPackets.Count.ToString()})");
			itemBackground.SetValueServer(regularColor);
		}

		public void Show()
		{
			seedExtractorWindow.SelectSeedType(seedPackets.First().name);
		}
	}
}
