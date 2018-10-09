using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public const float MinimumPressure = 0.00001f;
		public const float TileVolume = 2;

		public static bool IsPressureChanged(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.GetNeighbors())
			{
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