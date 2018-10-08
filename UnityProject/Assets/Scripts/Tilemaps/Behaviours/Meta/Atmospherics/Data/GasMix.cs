using System;
using System.Linq;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

namespace Atmospherics
{
	public struct GasMix
	{
		public readonly float[] Gases;

		public readonly float Pressure; // in kPA
		public readonly float Volume; // in m3

		public float Moles =>  Gases.Sum();
		public float Temperature => GasMixUtils.CalcTemperature(Pressure, Volume, Gases.Sum());

		private GasMix(float[] gases, float pressure, float volume = GasMixUtils.TileVolume)
		{
			Gases = gases;
			Pressure = pressure;
			Volume = volume;
		}

		public static GasMix FromTemperature(float[] gases, float temperature, float volume = GasMixUtils.TileVolume)
		{
			float moles = gases.Sum();

			float pressure = GasMixUtils.CalcPressure(volume, moles, temperature);

			return new GasMix(gases, pressure, volume);
		}

		public static GasMix FromPressure(float[] gases, float pressure, float volume = GasMixUtils.TileVolume)
		{
			return new GasMix(gases, pressure, volume);
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
			return Pressure * Gases[gas] / Moles;
		}

		public float GetMoles(Gas gas)
		{
			return Gases[gas];
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume / 1000} L";
		}
	}
}