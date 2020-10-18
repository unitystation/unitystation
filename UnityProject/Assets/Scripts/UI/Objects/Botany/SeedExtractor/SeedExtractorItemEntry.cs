using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Items.Botany;

namespace UI.Objects.Botany
{
	public class SeedExtractorItemEntry : DynamicEntry
	{
		[SerializeField]
		private Color regularColor = Color.gray;
		//[SerializeField]
		//private Color emptyStockColor = Color.red;
		private SeedPacket seedItem;
		private GUI_SeedExtractor seedExtractorWindow;
		[SerializeField]
		private NetLabel seedStats = null;
		//[SerializeField]
		//private NetLabel itemCount = null;
		//[SerializeField]
		//private NetPrefabImage itemIcon = null;
		[SerializeField]
		private NetColorChanger itemBackground = null;

		public void SetItem(SeedPacket seedPacket, GUI_SeedExtractor correspondingWindow)
		{
			seedItem = seedPacket;
			//var seedPacket = seedItem.GetComponent<SeedPacket>();
			seedExtractorWindow = correspondingWindow;
			seedStats.SetValueServer($"{seedPacket.plantData.Potency.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.Yield.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.GrowthSpeed.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.Endurance.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.Lifespan.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.WeedResistance.ToString().PadLeft(3)} " +
							   $"{seedPacket.plantData.WeedGrowthRate.ToString().PadLeft(3)}");
			itemBackground.SetValueServer(regularColor);
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
}
