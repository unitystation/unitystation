using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Facepunch.Steamworks;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine.Experimental.AI;

namespace Atmospherics
{
	public struct GasMix
	{
		private readonly float[] Gases;

		public readonly float Pressure; // in PA
		public readonly float Temperature; // in K
		public readonly float Moles;
		public readonly float Volume; // in m3

		public float GetGasAmount(Gas gas)
		{
			return Gases[gas.Index];
		}

		public GasMix(float[] gases, float temperature, float volume = 2)
		{
			Gases = gases;
			Moles = gases.Sum();

			Temperature = temperature;
			Volume = volume;

			Pressure = GasMixUtils.CalcPressure(volume, Moles, temperature);
		}

		public GasMix(float[] gases, float moles, float temperature, float pressure, float volume = 2)
		{
			Gases = gases;
			Moles = moles;
			Pressure = pressure;
			Temperature = temperature;
			Volume = volume;
		}

		public static GasMix operator +(GasMix a, GasMix b)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] + b.Gases[i];
			}

			float moles = a.Moles + b.Moles;
			float pressure = a.Pressure + b.Pressure * b.Volume / a.Volume;
			float temperature = GasMixUtils.CalcTemperature(pressure, a.Volume, moles);

			return new GasMix(gases, moles, temperature, pressure, a.Volume);
		}

		public static GasMix operator *(GasMix a, float factor)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] * factor;
			}

			float moles = a.Moles * factor;
			float pressure = a.Pressure * factor;

			return new GasMix(gases, moles, a.Temperature, pressure, a.Volume);
		}

		public static GasMix operator /(GasMix a, float factor)
		{
			float[] gases = new float[Gas.Count];

			for (int i = 0; i < Gas.Count; i++)
			{
				gases[i] = a.Gases[i] / factor;
			}

			float moles = a.Moles / factor;
			float pressure = a.Pressure / factor;

			return new GasMix(gases, moles, a.Temperature, pressure, a.Volume);
		}

		public static GasMix Sum(params GasMix[] gasMixes)
		{
			return Sum(GasMixUtils.TileVolume, gasMixes);
		}

		public static GasMix Sum(float volume, params GasMix[] gasMixes)
		{
			float[] gases = new float[Gas.Count];
			float moles = 0;
			float pressure = 0;

			foreach (GasMix gasMix in gasMixes)
			{
				for (int i = 0; i < Gas.Count; i++)
				{
					gases[i] += gasMix.Gases[i];
				}

				moles += gasMix.Moles;

				pressure += gasMix.Pressure * gasMix.Volume / volume;
			}

			float temperature = GasMixUtils.CalcTemperature(pressure, volume, moles);

			return new GasMix(gases, moles, temperature, pressure, volume);
		}

		public override string ToString()
		{
			return $"{Pressure/1000} kPA, {Temperature} K, {Moles} mol, {Volume/1000} L";
		}

		// TODO remove. Add energy method ?
	}
}