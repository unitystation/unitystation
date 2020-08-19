using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public static readonly Vector2Int MINUS_ONE = new Vector2Int(-1, -1);

		public static bool IsPressureChanged(MetaDataNode node, out Vector2Int windDirection, out float windForce)
		{
			MetaDataNode[] neighbors = node.Neighbors;
			windDirection = Vector2Int.zero;
			Vector3Int clampVector = Vector3Int.zero;
			windForce = 0L;
			bool result = false;

			for (var i = 0; i < neighbors.Length; i++)
			{
				MetaDataNode neighbor = neighbors[i];
				if (neighbor != null)
				{
					float pressureDifference = node.GasMix.Pressure - neighbor.GasMix.Pressure;
					float absoluteDifference = Mathf.Abs(pressureDifference);
					if (absoluteDifference > AtmosConstants.MinPressureDifference)
					{
						result = true;

						if (!neighbor.IsOccupied)
						{
							if (absoluteDifference > windForce)
							{
								windForce = absoluteDifference;
							}

							int neighborOffsetX = (neighbor.Position.x - node.Position.x);
							int neighborOffsetY = (neighbor.Position.y - node.Position.y);

							if (pressureDifference > 0)
							{
								windDirection.x += neighborOffsetX;
								windDirection.y += neighborOffsetY;
							}
							else if (pressureDifference < 0)
							{
								windDirection.x -= neighborOffsetX;
								windDirection.y -= neighborOffsetY;
							}

							clampVector.x -= neighborOffsetX;
							clampVector.y -= neighborOffsetY;
						}
					}
					else
					{
						// check if the moles are different. (e.g. CO2 is different from breathing)
						for (int j = 0; j < Gas.Count; j++)
						{
							float moles = node.GasMix.Gases[j];
							float molesNeighbor = neighbor.GasMix.Gases[j];

							if (Mathf.Abs(moles - molesNeighbor) > AtmosConstants.MinPressureDifference)
							{
								result = true;
							}
						}
					}
				}
			}

			//not blowing in direction of tiles that aren't atmos passable
			windDirection.y = Mathf.Clamp(windDirection.y, clampVector.y < 0 ? 0 : -1,
				clampVector.y > 0 ? 0 : 1);
			windDirection.x = Mathf.Clamp(windDirection.x, clampVector.x < 0 ? 0 : -1,
				clampVector.x > 0 ? 0 : 1);

			return result;
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
			if (temperature > 0 && pressure > 0 && volume > 0)
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

		public static float CalcHeatCapacity(float[] Gases)
		{
			float capacity = 0f;
			foreach (Gas gas in Gas.All)
			{
				capacity += gas.MolarHeatCapacity * Gases[gas];
			}

			return capacity;
		}
	}
}