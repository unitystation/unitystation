using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public static bool IsPressureChanged(MetaDataNode node)
		{
			MetaDataNode[] neighbors = node.Neighbors;

			for (var i = 0; i < neighbors.Length; i++)
			{
				MetaDataNode neighbor = neighbors[i];
				if (neighbor != null)
				{
					if (Mathf.Abs(node.GasMix.Pressure - neighbor.GasMix.Pressure) > AtmosConstants.MinPressureDifference)
					{
						return true;
					}

					// check if the moles are different. (e.g. CO2 is different from breathing)
					for (int j = 0; j < Gas.Count; j++)
					{
						float moles = node.GasMix.Gases[j];
						float molesNeighbor = neighbor.GasMix.Gases[j];

						if (Mathf.Abs(moles - molesNeighbor) > AtmosConstants.MinPressureDifference)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static float CalcPressure(float volume, float moles, float temperature)
		{
			if (temperature > 0 && moles > 0 && volume > 0)
			{
				return moles * Gas.R * temperature / volume / 1000;
			}

			return 0;
		}

		public static float CalcVolume(float pressure, float moles, float temperature)
		{
			if (temperature > 0 && pressure > 0 && moles > 0)
			{
				return moles * Gas.R * temperature / pressure;
			}

			return 0;
		}

		public static float CalcMoles(float pressure, float volume, float temperature)
		{
			if (temperature > 0 && pressure > 0  && volume > 0)
			{
				return pressure * volume / (Gas.R * temperature) * 1000;
			}

			return 0;
		}

		public static float CalcTemperature(float pressure, float volume, float moles)
		{
			if (volume > 0 && pressure > 0 && moles > 0)
			{
				return pressure * volume / (Gas.R * moles) * 1000;
			}

			return 0;
		}

	}
}