using System;

namespace Systems.Atmospherics
{
	//Very similliar reaction to hot ice formation but without the oxygen requirement and different parameters
	public class MetalHydrogenFormation : Reaction
	{
		private static Random rnd = new Random();

		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var energyNeeded = 0f;
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var temperatureScale = 0f;

			if (gasMix.Temperature < AtmosDefines.HYRDOGEN_MIN_CRYSTALLISE_TEMPERATURE)
			{
				temperatureScale = 0;
			}
			else
			{
				temperatureScale = (AtmosDefines.HYRDOGEN_MAX_CRYSTALLISE_TEMPERATURE - gasMix.Temperature) / (AtmosDefines.HYRDOGEN_MAX_CRYSTALLISE_TEMPERATURE - AtmosDefines.HYRDOGEN_MIN_CRYSTALLISE_TEMPERATURE);
			}

			if (temperatureScale >= 0)
			{
				var crystalliseRate = gasMix.GetMoles(Gas.Hydrogen) * temperatureScale / AtmosDefines.HYDROGEN_CRYSTALLISE_RATE;
				
				if (crystalliseRate > 0.0001f)
				{
					gasMix.RemoveGas(Gas.Hydrogen, crystalliseRate);

					if (gasMix.Temperature > AtmosDefines.HYRDOGEN_MIN_CRYSTALLISE_TEMPERATURE && gasMix.Temperature < AtmosDefines.HYRDOGEN_MAX_CRYSTALLISE_TEMPERATURE && rnd.Next(0, 2) == 0)
					{
						SpawnSafeThread.SpawnPrefab(node.Position.ToWorldInt(node.PositionMatrix), AtmosManager.Instance.metalHydrogen);
					}

					energyNeeded += AtmosDefines.HYRDOGEN_CRYSTALLISE_ENERGY * crystalliseRate;
				}
			}

			if (energyNeeded > 0)
			{
				var newHeatCap = gasMix.WholeHeatCapacity;

				if (newHeatCap > 0.0003f)
				{
					gasMix.SetTemperature((gasMix.Temperature * oldHeatCap - energyNeeded) / newHeatCap);
				}
			}
		}

	}
}
