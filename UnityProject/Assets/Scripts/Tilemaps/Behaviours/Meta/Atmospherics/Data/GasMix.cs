using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Pipes;
using UnityEngine;

namespace Systems.Atmospherics
{
	/// <summary>
	/// Represents a mix of gases
	/// </summary>
	[Serializable]
	public class GasMix
	{
		[InfoBox("Gas data container", EInfoBoxType.Normal)]
		public GasData GasData;
		public GasValues[] GasesArray => GasData.GasesArray;

		public float Pressure;// in kPA
		public float Volume; // in m3
		public float Temperature;

		public float Moles
		{
			get
			{
				float value = 0;
				foreach (var a in GasesArray)
				{
					value += a.Moles;
				}

				if (float.IsNaN(value))
				{
					return 0;
				}

				return value;
			}
		}

		public float WholeHeatCapacity //this is the heat capacity for the entire gas mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;

				foreach (var gas in GasesArray)
				{
					capacity += Gas.Get(gas.GasType).MolarHeatCapacity * gas.Moles;
				}

				return capacity;
			}
		}

		public float InternalEnergy //This is forgetting the amount of energy inside of the Gas
		{
			get => (WholeHeatCapacity * Temperature);

			set
			{
				if (WholeHeatCapacity == 0)
				{
					Temperature = 0;
				}
				else
				{
					Temperature = (value / WholeHeatCapacity);
				}
			}
		}

		public GasMix(float volume = AtmosConstants.TileVolume, float temperature = Reactions.KOffsetC + 20)
		{
			GasData = new GasData();
			Volume = volume;
			Temperature = temperature;
		}

		/// <summary>
		/// Changes the pressure by the specified value
		/// </summary>
		/// <param name="changePressure">The change of the pressure, can be + or - </param>
		public void ChangePressure(float changePressure)
		{
			SetPressure(Temperature + changePressure);
		}

		/// <summary>
		/// Changes the volume by the specified value
		/// </summary>
		/// <param name="changeVolume">The change of the volume, can be + or - </param>
		public void ChangeVolumeValue(float changeVolume)
		{
			Volume += changeVolume;
			RecalculatePressure();
		}

		/// <summary>
		/// Changes the temperature by the specified value
		/// </summary>
		/// <param name="changeTemperature">The change of the temperature, can be + or - </param>
		public void ChangeTemperature(float changeTemperature)
		{
			SetTemperature(Temperature + changeTemperature);
		}

		public void SetTemperature(float newTemperature)
		{
			Temperature = newTemperature;
			RecalculatePressure();
		}

		public void SetPressure(float newPressure)
		{
			Pressure = newPressure;
			Temperature = AtmosUtils.CalcTemperature(Pressure, Volume, Moles);
		}

		private void RecalculatePressure()
		{
			Pressure = AtmosUtils.CalcPressure(Volume, Moles, Temperature);
		}

		public static GasMix NewGasMix(GasMix other)
		{
			return FromPressure(other.GasData.Copy(), other.Pressure, other.Volume);
		}

		public static GasMix FromTemperature(GasData gases, float temperature, float volume = AtmosConstants.TileVolume)
		{
			float pressure = 0;

			if (temperature >= 0)
			{
				pressure = AtmosUtils.CalcPressure(volume, gases.Sum(), temperature);
			}

			return FromPressure(gases, pressure, volume);
		}

		public static GasMix FromPressure(GasData gases, float pressure,
			float volume = AtmosConstants.TileVolume)
		{
			var gaxMix = new GasMix();
			gaxMix.GasData = gases.Copy();
			gaxMix.Pressure = pressure;
			gaxMix.Volume = volume;
			gaxMix.Temperature = AtmosUtils.CalcTemperature(gaxMix.Pressure, gaxMix.Volume, gaxMix.GasData.Sum());
			return gaxMix;
		}

		/// <summary>
		/// Transfers moles from one gas to another
		/// </summary>
		public static void TransferGas(GasMix target, GasMix source, float molesTransferred)
		{
			var sourceStartMoles = source.Moles;
			if (CodeUtilities.IsEqual(molesTransferred, 0) || CodeUtilities.IsEqual(sourceStartMoles, 0))
				return;
			var percentage =  molesTransferred / sourceStartMoles;
			var targetStartMoles = target.Moles;

			foreach (var gas in Gas.Gases)
			{
				var sourceMoles = source.GetMoles(gas.Value);
				if (CodeUtilities.IsEqual(sourceMoles, 0)) continue;

				var transfer = sourceMoles * percentage;

				//Add to target
				target.GasData.ChangeMoles(gas.Key, transfer);

				//Remove from source
				source.GasData.ChangeMoles(gas.Key, -transfer);
			}

			if (CodeUtilities.IsEqual(target.Temperature, source.Temperature))
			{
				target.RecalculatePressure();
			}
			else
			{
				var energyTarget = targetStartMoles * target.Temperature;
				var energyTransfer = molesTransferred * source.Temperature;
				var targetTempFinal = (energyTransfer + energyTarget) / (targetStartMoles + molesTransferred);
				target.SetTemperature(targetTempFinal);
			}

			if (CodeUtilities.IsEqual(percentage, 1)) //transferred everything, source is empty
			{
				source.SetPressure(0);
			}
			else
			{
				source.RecalculatePressure();
			}
		}

		/// <summary>
		/// Source and target gas mixes interchange their moles
		/// </summary>
		/// <param name="otherGas"></param>
		public GasMix MergeGasMix(GasMix otherGas)
		{
			var totalInternalEnergy = InternalEnergy + otherGas.InternalEnergy;
			var totalWholeHeatCapacity = WholeHeatCapacity + otherGas.WholeHeatCapacity;
			var newTemperature = totalWholeHeatCapacity > 0 ? totalInternalEnergy / totalWholeHeatCapacity : 0;
			var totalVolume = Volume + otherGas.Volume;

			foreach (var gas in Gas.Gases)
			{
				var gasMoles = GasData.GetGasMoles(gas.Key);
				gasMoles += otherGas.GasData.GetGasMoles(gas.Key);
				gasMoles /= totalVolume;

				GasData.SetMoles(gas.Key, gasMoles * Volume);
				otherGas.GasData.SetMoles(gas.Key, gasMoles * otherGas.Volume);
			}

			SetTemperature(newTemperature);
			otherGas.SetTemperature(newTemperature);
			return otherGas;
		}

		public void MultiplyGas(float factor)
		{
			for (int i = 0; i < GasesArray.Length; i++)
			{
				GasesArray[i].Moles *= factor;
			}

			SetPressure(Pressure * factor);
		}

		public void SetToEmpty()
		{
			MultiplyGas(0);
		}

		public float GetPressure(Gas gas)
		{
			if (Moles == 0) return 0;

			return Pressure * (GetMoles(gas) / Moles);
		}

		public float GetMoles(Gas gas)
		{
			return GasData.GetGasMoles(gas.GasType);
		}

		/// <summary>
		/// Returns the gas as a percentage of the gas in the mix
		/// </summary>
		/// <returns>The ratio of the gas</returns>
		/// <param name="gasIndex">Gas index.</param>
		public float GasRatio(Gas gasIndex)
		{
			return GetMoles(gasIndex) / Moles;
		}

		/// <summary>
		///  Ensures that all containers have the same pressure
		/// </summary>
		/// <param name="otherGas"></param>
		public void MergeGasMixes(List<PipeData> otherGas)
		{
			float totalVolume = Volume;
			float totalInternalEnergy = InternalEnergy;
			float totalWholeHeatCapacity = WholeHeatCapacity;
			foreach (var gasMix in otherGas)
			{
				totalInternalEnergy += PipeFunctions.PipeOrNet(gasMix).GetGasMix().InternalEnergy;
				totalWholeHeatCapacity += PipeFunctions.PipeOrNet(gasMix).GetGasMix().WholeHeatCapacity;
				totalVolume += PipeFunctions.PipeOrNet(gasMix).GetGasMix().Volume;
			}

			var newTemperature = totalInternalEnergy / totalWholeHeatCapacity;

			foreach (var gas in Gas.Gases)
			{
				var gasMoles = GasData.GetGasMoles(gas.Key);

				foreach (var gasMix in otherGas)
				{
					gasMoles += PipeFunctions.PipeOrNet(gasMix).GetGasMix().GasData.GetGasMoles(gas.Key);;
				}

				gasMoles /= totalVolume;
				GasData.SetMoles(gas.Key, gasMoles * Volume);

				foreach (var gasMix in otherGas)
				{
					var inGas = PipeFunctions.PipeOrNet(gasMix).GetGasMix();
					inGas.GasData.SetMoles(gas.Key, gasMoles * inGas.Volume);
					PipeFunctions.PipeOrNet(gasMix).SetGasMix(inGas);
				}
			}

			SetTemperature(newTemperature);
			foreach (var pipeData in otherGas)
			{
				var getMixAndVolume = pipeData.GetMixAndVolume;
				var gasMix = getMixAndVolume.GetGasMix();
				gasMix.SetTemperature(newTemperature);
				getMixAndVolume.SetGasMix(gasMix);
			}
		}

		/// <summary>
		/// Set the moles value of a gas inside of a GasMix.
		/// </summary>
		/// <param name="gas">The gas you want to set.</param>
		/// <param name="moles">The amount to set the gas.</param>
		public void SetGas(Gas gas, float moles)
		{
			GasData.SetMoles(gas.GasType, moles);
			RecalculatePressure();
		}

		public void AddGas(Gas gas, float moles)
		{
			GasData.ChangeMoles(gas.GasType, moles);
			RecalculatePressure();
		}

		public void RemoveGas(Gas gas, float moles)
		{
			GasData.ChangeMoles(gas.GasType, -moles);
			RecalculatePressure();
		}

		public void Copy(GasMix other)
		{
			GasData = other.GasData.Copy();
			Pressure = other.Pressure;
			Temperature = other.Temperature;
			Volume = other.Volume;
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume}m^3 ";
		}
	}
}
