using System;
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
		public readonly float[] Gases;

		public readonly float Pressure; // in kPA
		public readonly float Temperature; // in K
		public readonly float Moles;
		public readonly float Volume; // in m3

		public GasMix(float[] gases, float temperature, float volume = 2)
		{
			this.Gases = gases;
			Moles = gases.Sum();

			Temperature = temperature;
			Volume = volume;

			Pressure = GasMixUtils.CalcPressure(volume, Moles, temperature);
		}

		private GasMix(float[] gases, float moles, float temperature, float pressure, float volume = 2)
		{
			this.Gases = gases;
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

		public float GetPressure(Gas gas)
		{
			return Pressure * Gases[gas] / Moles;
		}

		public float GetMoles(Gas gas)
		{
			return Gases[gas];
		}

		public void SetVolume(float volume)
		{
			this = new GasMix(Gases, Temperature, volume);
		}

		public void SetTemperature(float temperature)
		{
			this = new GasMix(Gases, temperature, Volume);
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume/1000} L";
		}
	}
}