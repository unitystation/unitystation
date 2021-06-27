using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Systems.Atmospherics
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
						foreach (var gas in Gas.Gases)
						{
							float moles = node.GasMix.GasData.GetGasMoles(gas.Key);
							float molesNeighbor = neighbor.GasMix.GasData.GetGasMoles(gas.Key);

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

		/// <summary>
		/// Total moles of this array of gases
		/// </summary>
		public static float Sum(this GasData data)
		{
			var total = 0f;

			foreach (var gas in data.GasesArray)
			{
				total += gas.Moles;
			}

			return total;
		}

		/// <summary>
		/// Checks to see if the data contains a gas
		/// </summary>
		public static bool HasGasType(this GasData data, GasType gasType)
		{
			return data.GasesDict.ContainsKey(gasType);
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array
		/// </summary>
		public static float GetGasMoles(this GasData data, GasType gasType)
		{
			return GetGasType(data, gasType)?.Moles ?? 0;
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array
		/// </summary>
		public static void GetGasMoles(this GasData data, GasType gasType, out float gasMoles)
		{
			gasMoles = GetGasMoles(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array
		/// </summary>
		public static void GetGasType(this GasData data, GasType gasType, out GasValues gasData)
		{
			gasData = GetGasType(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array
		/// </summary>
		public static GasValues GetGasType(this GasData data, GasType gasType)
		{
			if (data.GasesDict.TryGetValue(gasType, out var value))
			{
				return value;
			}

			return null;
		}

		/// <summary>
		/// Adds/Removes moles for a specific gas in the gas data
		/// </summary>
		public static void ChangeMoles(this GasData data, GasType gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, true);
		}

		/// <summary>
		/// Sets moles for a specific gas to a specific value in the gas data
		/// </summary>
		public static void SetMoles(this GasData data, GasType gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, false);
		}

		private static void InternalSetMoles(GasData data, GasType gasType, float moles, bool isChange)
		{
			GetGasType(data, gasType, out var gas);

			if (gas != null)
			{
				if (isChange)
				{
					gas.Moles += moles;
				}
				else
				{
					gas.Moles = moles;
				}

				if (gas.Moles < 0)
				{
					data.RemoveGasType(gasType);
				}

				return;
			}

			//Dont add new data for negative moles or if approx 0
			if(Math.Sign(moles) == -1 || moles.Approx(0)) return;

			var newValues = new GasValues {Moles = moles, GasType = gasType};
			var newArray = new GasValues[data.GasesArray.Length + 1];

			for (int i = 0; i < newArray.Length; i++)
			{
				if (data.GasesArray.Length == i)
				{
					newArray[i] = newValues;

					//Should only happen on last index since we are adding only one thing so can break
					break;
				}

				newArray[i] = data.GasesArray[i];
			}

			data.GasesArray = newArray;
			data.GasesDict.Add(gasType, newValues);
		}

		/// <summary>
		/// Removes a specific gas type
		/// </summary>
		public static void RemoveGasType(this GasData data, GasType gasType)
		{
			var newData = new GasValues[data.GasesArray.Length - 1];
			var count = 0;

			foreach (var gas in data.GasesArray)
			{
				if(gas.GasType == gasType) continue;

				newData[count] = gas;
				count++;
			}

			data.GasesArray = newData;
			data.GasesDict.Remove(gasType);
		}

		/// <summary>
		/// Copies the array, creating new references
		/// </summary>
		/// <param name="oldData"></param>
		public static GasData Copy(this GasData oldData)
		{
			var newGasData = new GasData();

			foreach (var value in oldData.GasesArray)
			{
				newGasData.SetMoles(value.GasType, value.Moles);
			}

			newGasData.RegenerateDict();

			return newGasData;
		}
	}
}