namespace Atmospherics
{
	public static class GasMixes
	{
		public static readonly GasMix Space;
		public static readonly GasMix Air;
		public static readonly GasMix Empty;

		static GasMixes()
		{
			float[] gases = new float[Gas.Count];
			gases[Gas.Oxygen] = 0.001f;
			gases[Gas.CarbonDioxide] = 0.01f;
			gases[Gas.Nitrogen] = 0;
			Space = GasMix.FromTemperature(gases, 2.7f);

			gases[Gas.CarbonDioxide] = 0;
			gases[Gas.Oxygen] = 16.628484400890768491815384755837f / 2 * 2.5f;
			gases[Gas.Nitrogen] = 66.513937603563073967261539023347f / 2 * 2.5f;

			Air = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);

			gases[Gas.Oxygen] = 0;
			gases[Gas.Nitrogen] = 0;
			Empty = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);
		}
	}
}