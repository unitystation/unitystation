using Atmospherics;

namespace Tilemaps.Behaviours.Meta.Utils
{
	public static class GasMixUtils
	{
		public const float TileVolume = 2;

		public static GasMix Space = new GasMix(new float[Gas.Count], 2.7f);
		public static GasMix Air;

		static GasMixUtils()
		{
			float[] gases = new float[Gas.Count];
			gases[Gas.Oxygen] = 16.628484400890768491815384755837f;
			gases[Gas.Nitrogen] = 66.513937603563073967261539023347f;

			Air = new GasMix(gases, 293.15f);
		}

		public static float CalcPressure(float volume, float moles, float temperature)
		{
			return moles * Gas.R * temperature / volume / 1000;
		}

		public static float CalcVolume(float pressure, float moles, float temperature)
		{
			return moles * Gas.R * temperature / pressure;
		}

		public static float CalcMoles(float pressure, float volume, float temperature)
		{
			return pressure * volume / (Gas.R * temperature) * 1000;
		}

		public static float CalcTemperature(float pressure, float volume, float moles)
		{
			return pressure * volume / (Gas.R * moles) * 1000;
		}
	}
}