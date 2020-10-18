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
	public struct GasMix
	{
		[InfoBox("Plasma, oxygen, nitrogen, carbon dioxide", EInfoBoxType.Normal)]
		public float[] Gases;

		public float Pressure;// in kPA
		public float Volume; // in m3

		public float Moles => Gases.Sum();

		public float Temperature { get; private set; }

		public void SetTemperature(float newTemperature)
		{
			Temperature = newTemperature;
			Pressure = AtmosUtils.CalcPressure(Volume, Moles, Temperature);
		}

		public void SetPressure(float newPressure)
		{
			Pressure = newPressure;
			Temperature = AtmosUtils.CalcTemperature(Pressure, Volume, Moles);
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
			get { return (WholeHeatCapacity * Temperature); }

			set {
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

		private GasMix(float[] gases, float pressure, float volume = AtmosConstants.TileVolume)
		{
			Gases = gases;
			Pressure = pressure;
			Volume = volume;
			Temperature = AtmosUtils.CalcTemperature(Pressure, Volume, Gases.Sum());
		}

		public GasMix(GasMix other)
		{
			this = FromPressure((float[]) other.Gases.Clone(), other.Pressure, other.Volume);
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
			return new GasMix(gases.ToArray(), pressure, volume);
		}

		public static GasMix operator +(GasMix a, GasMix b)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] + b.Gases[i];
			}

			float pressure = a.Pressure + b.Pressure * b.Volume / a.Volume;

			return new GasMix(gases, pressure, a.Volume);
		}

		public static GasMix operator -(GasMix a, GasMix b)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] - b.Gases[i];
			}

			float pressure = a.Pressure - b.Pressure * b.Volume / a.Volume;

			return new GasMix(gases, pressure, a.Volume);
		}

		public static GasMix operator *(GasMix a, float factor)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] * factor;
			}

			float pressure = a.Pressure * factor;

			return new GasMix(gases, pressure, a.Volume);
		}

		public static GasMix operator /(GasMix a, float factor)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] / factor;
			}

			float pressure = a.Pressure / factor;

			return new GasMix(gases, pressure, a.Volume);
		}

		public float GetPressure(Gas gas)
		{
			return Moles > 0 ? Pressure * Gases[gas] / Moles : 0;
		}

		public float GetMoles(Gas gas)
		{
			return Gases[gas];
		}

		public void ChangeVolumeValue(float value)
		{
			Volume += value;
			Recalculate();
		}

		public GasMix RemoveMoles(float InMoles)
		{
			if (InMoles == 0)
			{
				return new GasMix(GasMixes.Empty);
			}

			if (Moles == 0)
			{
				Logger.LogError("OH GOD IS 0 RTOOO!!");
				return new GasMix(GasMixes.Empty);
				;
			}

			if (InMoles > Moles)
			{
				InMoles = Moles;
			}

			return RemoveRatio(InMoles / Moles);
		}

		public GasMix RemoveVolume(float volume, bool setVolume = false)
		{
			GasMix removed = RemoveRatio(volume / Volume);

			if (setVolume)
			{
				removed.Volume = volume;
				removed = FromTemperature(removed.Gases, Temperature, volume);
			}

			return removed;
		}

		public GasMix RemoveRatio(float ratio)
		{
			GasMix removed = this * ratio;

			for (int i = 0; i < Gas.Count; i++)
			{
				Gases[i] -= removed.Gases[i];
			}

			SetPressure(Pressure -= removed.Pressure * removed.Volume / Volume);

			return removed;
		}


		/// <summary>
		/// Returns the gas as a percentage of the gas in the mix
		/// </summary>
		/// <returns>The ratio of the gas</returns>
		/// <param name="_Gas">Gas.</param>
		public float GasRatio(Gas _Gas)
		{
			if (Gases[_Gas] != 0)
			{
				return (Gases[_Gas] / Moles);
			}
			else
			{
				return (0);
			}
		}


		/// <summary>
		/// Ensures that both containers have the same pressure
		/// </summary>
		/// <param name="otherGas"></param>
		public GasMix MergeGasMix(GasMix otherGas)
		{
			float totalInternalEnergy = InternalEnergy + otherGas.InternalEnergy;
			float totalWholeHeatCapacity = WholeHeatCapacity + otherGas.WholeHeatCapacity;
			float Newtemperature = totalInternalEnergy / totalWholeHeatCapacity;
			float totalVolume = Volume + otherGas.Volume;
			for (int i = 0; i < Gas.Count; i++)
			{
				if (Gases[i] < 0)
				{
					Debug.Log("OH GOFD!!");
				}

				float gas = (Gases[i] + otherGas.Gases[i]) / totalVolume;
				Gases[i] = gas * Volume;
				otherGas.Gases[i] = gas * otherGas.Volume;
			}

			SetTemperature(Newtemperature);
			otherGas.SetTemperature(Newtemperature);
			return otherGas;
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


			float Newtemperature = totalInternalEnergy / totalWholeHeatCapacity;
			for (int i = 0; i < Gas.Count; i++)
			{
				float gas = (Gases[i]);
				foreach (var gasMix in otherGas)
				{
					gas += PipeFunctions.PipeOrNet(gasMix).GetGasMix().Gases[i];
				}

				gas /= totalVolume;
				Gases[i] = gas * Volume;
				foreach (var gasMix in otherGas)
				{
					var Ingas = PipeFunctions.PipeOrNet(gasMix).GetGasMix();
					Ingas.Gases[i] = gas * Ingas.Volume;
					PipeFunctions.PipeOrNet(gasMix).SetGasMix(Ingas);
				}
			}

			SetTemperature(Newtemperature);
			foreach (var gasMix in otherGas)
			{
				var getMixAndVolume = gasMix.GetMixAndVolume;
				var gasm = getMixAndVolume.GetGasMix();
				gasm.SetTemperature(Newtemperature);
				getMixAndVolume.SetGasMix(gasm);

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
			Recalculate();
		}

		public void AddGas(Gas gas, float moles)
		{
			Gases[gas] += moles;
			Recalculate();
		}

		public GasMix AddGasReturn(Gas gas, float moles)
		{
			Gases[gas] += moles;
			Recalculate();
			return (this);
		}

		public void RemoveGas(Gas gas, float moles)
		{
			Gases[gas] -= moles;
			if (Gases[gas] < 0)  Gases[gas] = 0;
			Recalculate();
		}

		public GasMix RemoveGasReturn(Gas gas, float moles)
		{
			Gases[gas] -= moles;
			Recalculate();
			return (this);
		}

		public void Copy(GasMix other)
		{
			for (int i = 0; i < Gas.Count; i++)
			{
				Gases[i] = other.Gases[i];
			}

			SetPressure(other.Pressure);
			Volume = other.Volume;
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume}m^3 ";
		}

		private void Recalculate()
		{
			Pressure = AtmosUtils.CalcPressure(Volume, Moles, Temperature);
		}
	}
}
