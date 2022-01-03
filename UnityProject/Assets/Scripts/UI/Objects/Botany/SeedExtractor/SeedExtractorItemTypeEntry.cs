using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UI.Core.NetUI;
using Items.Botany;

namespace UI.Objects.Botany
{
	public class SeedExtractorItemTypeEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;
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

		private List<SeedPacket> seedPackets;

		public void SetItem(List<SeedPacket> item, GUI_SeedExtractor correspondingWindow)
		{
			seedPackets = item;
			seedExtractorWindow = correspondingWindow;
			itemName.SetValueServer(seedPackets.First().name);
			itemIcon.SetValueServer(seedPackets.First().name);
			itemCount.SetValueServer($"({seedPackets.Count})");
			itemBackground.SetValueServer(regularColor);
		}

		public void Show()
		{
			seedExtractorWindow.SelectSeedType(seedPackets.First().name);
		}
	}
}
