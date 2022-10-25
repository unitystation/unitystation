using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Items.Botany;

namespace UI.Objects.Botany
{
	public class SeedExtractorItemEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;

		[SerializeField]
		private NetText_label seedStats = null;

		[SerializeField]
		private NetColorChanger itemBackground = null;

		private SeedPacket seedItem;
		private GUI_SeedExtractor seedExtractorWindow;

		public void SetItem(SeedPacket seedPacket, GUI_SeedExtractor correspondingWindow)
		{
			seedItem = seedPacket;
			seedExtractorWindow = correspondingWindow;
			seedStats.MasterSetValue(
					$"{seedPacket.plantData.Potency.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.Yield.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.GrowthSpeed.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.Endurance.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.Lifespan.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.WeedResistance.ToString().PadLeft(3)} " +
					$"{seedPacket.plantData.WeedGrowthRate.ToString().PadLeft(3)}");
			itemBackground.MasterSetValue(regularColor);
		}

		public void Dispense()
		{
			if (seedItem == null || seedExtractorWindow == null) return;

			seedExtractorWindow.DispenseSeedPacket(seedItem);
		}
	}
}
