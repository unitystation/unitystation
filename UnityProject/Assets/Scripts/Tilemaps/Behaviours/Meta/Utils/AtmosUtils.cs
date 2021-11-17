using System;
using System.Collections.Generic;
using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class AtmosUtils
	{
		public static List<GasValues> PooledGasValues = new List<GasValues>();

		public static GasValues GetGasValues()
		{
			lock (PooledGasValues)
			{
				if (PooledGasValues.Count > 0)
				{
					var QEntry = PooledGasValues[0];
					PooledGasValues.RemoveAt(0);
					return QEntry;
				}
			}

			return new GasValues();
		}

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
				if (neighbor == null) continue;

				//We only need to check open tiles
				if (neighbor.IsOccupied) continue;

				float pressureDifference = node.GasMix.Pressure - neighbor.GasMix.Pressure;
				float absoluteDifference = Mathf.Abs(pressureDifference);

				//Check to see if theres a large pressure difference
				if (absoluteDifference > AtmosConstants.MinPressureDifference)
				{
					result = true;

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

					//We continue here so we can calculate the whole wind direction from all possible nodes
					continue;
				}

				//Check if the moles are different. (e.g. CO2 is different from breathing)
				//Check current node then check neighbor so we dont miss a gas if its only on one of the nodes

				//Current node
				//Only need to check if false
				if (result == false)
				{
					foreach (var gas in node.GasMix.Gases)
					{
						float moles = node.GasMix.GasData.GetGasMoles(gas.GasSO);
						float molesNeighbor = neighbor.GasMix.GasData.GetGasMoles(gas.GasSO);

						if (Mathf.Abs(moles - molesNeighbor) > AtmosConstants.MinPressureDifference)
						{
							result = true;

							//We break not return here so we can still work out wind direction
							break;
						}
					}
				}

				//Neighbor node
				//Only need to check if false
				if (result == false)
				{
					foreach (var gas in neighbor.GasMix.Gases) //doesn't appear to modify list while iterating
					{
						float moles = node.GasMix.GasData.GetGasMoles(gas.GasSO);
						float molesNeighbor = neighbor.GasMix.GasData.GetGasMoles(gas.GasSO);

						if (Mathf.Abs(moles - molesNeighbor) > AtmosConstants.MinPressureDifference)
						{
							result = true;

							//We break not return here so we can still work out wind direction
							break;
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

			return AtmosDefines.SPACE_TEMPERATURE; //space radiation
		}

		/// <summary>
		/// Total moles of this array of gases
		/// </summary>
		public static float Sum(this GasData data)
		{
			var total = 0f;

			foreach (var gas in data.Gases)
			{
				total += gas.Moles;
			}

			return total;
		}

		/// <summary>
		/// Checks to see if the gas mix contains a specific gas
		/// </summary>
		public static bool HasGasType(this GasData data, GasSO gasType)
		{
			return data.GasesDict.ContainsKey(gasType);
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static float GetGasMoles(this GasData data, GasSO gasType)
		{
			return GetGasType(data, gasType)?.Moles ?? 0;
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static float GetGasMoles(this GasData data, int gasType)
		{
			return GetGasType(data, gasType)?.Moles ?? 0;
		}

		/// <summary>
		/// Gets moles of a specific gas from the gas array, returns 0 if gas isn't in mix
		/// </summary>
		public static void GetGasMoles(this GasData data, GasSO gasType, out float gasMoles)
		{
			gasMoles = GetGasMoles(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static void GetGasType(this GasData data, GasSO gasType, out GasValues gasData)
		{
			gasData = GetGasType(data, gasType);
		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static GasValues GetGasType(this GasData data, GasSO gasType)
		{
			if (data.GasesDict.TryGetValue(gasType, out var value))
			{
				return value;
			}

			return null;
		}

		/// <summary>
		/// Gets a specific gas from the gas array, returns null if gas isn't in mix
		/// </summary>
		public static GasValues GetGasType(this GasData data, int gasType)
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
		public static void ChangeMoles(this GasData data, GasSO gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, true);
		}

		/// <summary>
		/// Sets moles for a specific gas to a specific value in the gas data
		/// </summary>
		public static void SetMoles(this GasData data, GasSO gasType, float moles)
		{
			InternalSetMoles(data, gasType, moles, false);
		}

		private static void InternalSetMoles(GasData data, GasSO gasType, float moles, bool isChange)
		{
			//Try to get gas value if already inside mix
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

				//Remove gas from mix if less than threshold
				if (gas.Moles <= AtmosConstants.MinPressureDifference)
				{
					data.RemoveGasType(gasType);
				}

				return;
			}

			//Gas isn't inside mix so we'll add it

			//Dont add new data for negative moles
			if (Math.Sign(moles) == -1) return;

			//Dont add if approx 0 or below threshold
			if (moles.Approx(0) || moles <= AtmosConstants.MinPressureDifference) return;

			var newValues = GetGasValues();
			newValues.Moles = moles;
			newValues.GasSO = gasType;

			data.Gases.Add(newValues);
			data.GasesDict.Add(gasType, newValues);
		}

		/// <summary>
		/// Removes a specific gas type
		/// </summary>
		public static void RemoveGasType(this GasData data, GasSO gasType)
		{
			if (data.GasesDict.TryGetValue(gasType, out var toRemove))
			{
				toRemove.Pool();

				//This GC's, will have to see how bad
				data.Gases.Remove(toRemove);

				data.GasesDict.Remove(gasType);
			}
		}

		/// <summary>
		/// Copies the array, creating new references
		/// </summary>
		/// <param name="oldData"></param>
		public static GasData Copy(this GasData oldData)
		{
			var newGasData = new GasData();

			foreach (var value in oldData.Gases)
			{
				newGasData.SetMoles(value.GasSO, value.Moles);
			}

			newGasData.RegenerateDict();

			return newGasData;
		}


		/// <summary>
		/// Copies the array, creating new references
		/// </summary>
		/// <param name="oldData"></param>
		public static void CopyTo(this GasData oldData, GasData copyTo)
		{
			copyTo.Clear();

			foreach (var value in oldData.Gases)
			{
				copyTo.SetMoles(value.GasSO, value.Moles);
			}

			copyTo.RegenerateDict();
		}
	}
}