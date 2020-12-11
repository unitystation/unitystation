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
		[InfoBox("Plasma, oxygen, nitrogen, carbon dioxide", EInfoBoxType.Normal)]
		public float[] Gases;

		public float Pressure;// in kPA
		public float Volume; // in m3
		public float Temperature;

		public float Moles
		{
			get
			{
				float value = 0;
				foreach (var a in Gases)
				{
					value += a;
				}
				return value;
			}
		}

		public float WholeHeatCapacity //this is the heat capacity for the entire gas mixture, in Joules/Kelvin. gets very big with lots of gas.
		{
			get
			{
				float capacity = 0f;
				foreach (Gas gas in Gas.All)
				{
					capacity += gas.MolarHeatCapacity * Gases[gas];
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
			return FromPressure((float[]) other.Gases.Clone(), other.Pressure, other.Volume);
		}

		public static GasMix FromTemperature(float[] gases, float temperature, float volume = AtmosConstants.TileVolume)
		{
			float pressure = 0;

			if (temperature >= 0)
			{
				pressure = AtmosUtils.CalcPressure(volume, gases.Sum(), temperature);
			}

			return FromPressure(gases, pressure, volume);
		}

		public static GasMix FromPressure(IEnumerable<float> gases, float pressure,
			float volume = AtmosConstants.TileVolume)
		{
			var gaxMix = new GasMix();
			gaxMix.Gases = gases.ToArray();
			gaxMix.Pressure = pressure;
			gaxMix.Volume = volume;
			gaxMix.Temperature = AtmosUtils.CalcTemperature(gaxMix.Pressure, gaxMix.Volume, gaxMix.Gases.Sum());
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

			for (int i = 0; i < Gas.Count; i++)
			{
				if (CodeUtilities.IsEqual(source.Gases[i], 0))
					continue;
				var transfer = source.Gases[i] * percentage;
				target.Gases[i] += transfer;
				source.Gases[i] -= transfer;
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
			for (var i = 0; i < Gas.Count; i++)
			{
				float gas = (Gases[i] + otherGas.Gases[i]) / totalVolume;
				Gases[i] = gas * Volume;
				otherGas.Gases[i] = gas * otherGas.Volume;
			}
			SetTemperature(newTemperature);
			otherGas.SetTemperature(newTemperature);
			return otherGas;
		}



		public void MultiplyGas(float factor)
		{
			for (int i = 0; i < Gas.Count; i++)
			{
				Gases[i] *= factor;
			}
			SetPressure(Pressure * factor);
		}

		public void SetToEmpty()
		{
			MultiplyGas(0);
		}

		public float GetPressure(Gas gas)
		{
			if (Moles == 0)
				return 0;
			return Pressure * (Gases[gas] / Moles);
		}

		public float GetMoles(Gas gas)
		{
			return Gases[gas];
		}

		public void ChangeVolumeValue(float value)
		{
			Volume += value;
			RecalculatePressure();
		}

		/// <summary>
		/// Returns the gas as a percentage of the gas in the mix
		/// </summary>
		/// <returns>The ratio of the gas</returns>
		/// <param name="gasIndex">Gas index.</param>
		public float GasRatio(Gas gasIndex)
		{
			if (Gases[gasIndex] != 0)
			{
				return (Gases[gasIndex] / Moles);
			}
			return 0;
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
			for (var i = 0; i < Gas.Count; i++)
			{
				var gas = (Gases[i]);
				foreach (var gasMix in otherGas)
				{
					gas += PipeFunctions.PipeOrNet(gasMix).GetGasMix().Gases[i];
				}

				gas /= totalVolume;
				Gases[i] = gas * Volume;
				foreach (var gasMix in otherGas)
				{
					var inGas = PipeFunctions.PipeOrNet(gasMix).GetGasMix();
					inGas.Gases[i] = gas * inGas.Volume;
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
			Gases[gas] = moles;
			RecalculatePressure();
		}

		public void AddGas(Gas gas, float moles)
		{
			Gases[gas] += moles;
			RecalculatePressure();
		}

		public void RemoveGas(Gas gas, float moles)
		{
			Gases[gas] -= moles;
			if (Gases[gas] < 0)
				Gases[gas] = 0;
			RecalculatePressure();
		}

		public void Copy(GasMix other)
		{
			for (int i = 0; i < Gas.Count; i++)
			{
				Gases[i] = other.Gases[i];
			}
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
