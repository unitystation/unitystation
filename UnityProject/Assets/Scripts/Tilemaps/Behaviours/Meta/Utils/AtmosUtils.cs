using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public const float MinimumPressure = 0.00001f;
		public const float TileVolume = 2.5f;

		public static bool IsPressureChanged(MetaDataNode node)
		{
			MetaDataNode[] neighbors = node.Neighbors;

			for (var i = 0; i < neighbors.Length; i++)
			{
				MetaDataNode neighbor = neighbors[i];
				if (Mathf.Abs(node.Atmos.Pressure - neighbor.Atmos.Pressure) > MinimumPressure)
				{
					return true;
				}
			}

			return false;
		}

		public static float CalcPressure(float volume, float moles, float temperature)
		{
			return moles * Gas.R * temperature / volume / 1000;
		}

		public static float CalcVolume(float pressure, float moles, float temperature)
		{
			return moles * Gas.R * temperature / pressure;
		}

		public static float CalcMoles(float pressure, float volume, float temperature)
		{
			return pressure * volume / (Gas.R * temperature) * 1000;
		}

		public static float CalcTemperature(float pressure, float volume, float moles)
		{
			return pressure * volume / (Gas.R * moles) * 1000;
		}

	}
}