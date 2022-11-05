using System;

namespace Systems.Atmospherics
{
	//Very similliar reaction to hot ice formation but without the oxygen requirement and different parameters
	public class MetalHydrogenFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var energyNeeded = 0f;
			var oldHeatCap = gasMix.WholeHeatCapacity;

			int numberOfBarsToSpawn = (int)(gasMix.GetMoles(Gas.Hydrogen) / AtmosDefines.HYDROGEN_CRYSTALLISE_RATE);

			int stackSize = 50;

			Math.Clamp(numberOfBarsToSpawn, 0, stackSize);

			if (numberOfBarsToSpawn < 1) return;
				
			gasMix.RemoveGas(Gas.Hydrogen, numberOfBarsToSpawn);

			SpawnSafeThread.SpawnPrefab(node.LocalPosition.ToWorldInt(node.PositionMatrix), AtmosManager.Instance.MetalHydrogen, amountIfStackable: numberOfBarsToSpawn);

			energyNeeded += AtmosDefines.HYRDOGEN_CRYSTALLISE_ENERGY * numberOfBarsToSpawn;
			

			if (energyNeeded > 0)
			{
				var newHeatCap = gasMix.WholeHeatCapacity;

				if (newHeatCap > 0f)
				{
					gasMix.SetTemperature((gasMix.Temperature * oldHeatCap - energyNeeded) / newHeatCap);
				}
			}
		}

	}
}
