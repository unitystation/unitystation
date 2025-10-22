using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UI.Core.NetUI;
using Items.Botany;
using Objects.Botany;

namespace UI.Objects.Botany
{
	public class SeedExtractorItemTypeEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;
		[SerializeField]
		private GUI_SeedExtractor seedExtractorWindow;
		[SerializeField]
		private NetText_label itemName = null;
		[SerializeField]
		private NetText_label itemCount = null;
		[SerializeField]
		private NetPrefabImage itemIcon = null;
		[SerializeField]
		private NetColorChanger itemBackground = null;

		private List<SeedExtractor.SeedAndPlantData> seedPackets;

		public void SetItem(List<SeedExtractor.SeedAndPlantData> item, GUI_SeedExtractor correspondingWindow)
		{
			seedPackets = item;
			seedExtractorWindow = correspondingWindow;
			itemName.MasterSetValue(seedPackets.First().SeedPacket.name);
			itemIcon.MasterSetValue(seedPackets.First().SeedPacket.name);
			itemCount.MasterSetValue($"({seedPackets.Count})");
			itemBackground.MasterSetValue(regularColor);
		}

		public void Show()
		{
			seedExtractorWindow.SelectSeedType(seedPackets.First().SeedPacket.name);
		}
	}
}
