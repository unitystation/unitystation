using UnityEngine;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Botany.SeedExtractor
{
	public class SeedExtractorItemEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;

		[SerializeField]
		private NetText_label seedStats = null;

		[SerializeField]
		private NetColorChanger itemBackground = null;

		private US13.Objects.Botany.SeedExtractor.SeedAndPlantData SeedAndPlantData;
		private GUI_SeedExtractor seedExtractorWindow;

		public void SetItem(US13.Objects.Botany.SeedExtractor.SeedAndPlantData InSeedAndPlantData, GUI_SeedExtractor correspondingWindow)
		{
			SeedAndPlantData = InSeedAndPlantData;
			seedExtractorWindow = correspondingWindow;
			seedStats.MasterSetValue(
					$"{InSeedAndPlantData.PlantData.Potency.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.Yield.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.GrowthSpeed.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.Endurance.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.Lifespan.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.WeedResistance.ToString().PadLeft(3)} " +
					$"{InSeedAndPlantData.PlantData.WeedGrowthRate.ToString().PadLeft(3)}");
			itemBackground.MasterSetValue(regularColor);
		}

		public void Dispense()
		{
			if (SeedAndPlantData == null || seedExtractorWindow == null) return;

			seedExtractorWindow.DispenseSeedPacket(SeedAndPlantData);
		}
	}
}
