using System.Linq;

namespace Atmospherics
{
	public struct GasMix
	{
		public readonly float[] Gases;

		public float Pressure { get; private set; } // in kPA
		public float Volume { get; private set; } // in m3

		public float Moles => Gases.Sum();

		public float Temperature
		{
			get { return AtmosUtils.CalcTemperature(Pressure, Volume, Moles); }
			set { Pressure = AtmosUtils.CalcPressure(Volume, Moles, value); }
		}

		public float HeatCapacity
		{
			get
			{
				float capacity = 0f;
				foreach (Gas gas in Gas.All)
				{
					capacity += gas.SpecificHeat * Gases[gas];
				}

				return capacity;
			}
		}

		private GasMix(float[] gases, float pressure, float volume = AtmosUtils.TileVolume)
		{
			Gases = gases;
			Pressure = pressure;
			Volume = volume;
		}

		public static GasMix FromTemperature(float[] gases, float temperature, float volume = AtmosUtils.TileVolume)
		{
			float moles = gases.Sum();

			float pressure = AtmosUtils.CalcPressure(volume, moles, temperature);

			return new GasMix(gases, pressure, volume);
		}

		public static GasMix FromPressure(float[] gases, float pressure, float volume = AtmosUtils.TileVolume)
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

		public GasMix RemoveVolume(float volume)
		{
			return RemoveRatio(volume / Volume);
		}

		public GasMix RemoveRatio(float ratio)
		{
			GasMix removed = this * ratio;

			this -= removed;

			return removed;
		}

		public void AddGas(Gas gas, float moles)
		{
			Gases[gas] += moles;

			Recalculate();
		}

		public void RemoveGas(Gas gas, float moles)
		{
			Gases[gas] -= moles;

			Recalculate();
		}

		public override string ToString()
		{
			return $"{Pressure} kPA, {Temperature} K, {Moles} mol, {Volume * 1000} L";
		}

		private void Recalculate()
		{
			Pressure = AtmosUtils.CalcPressure(Volume, Moles, Temperature);
		}
	}
}