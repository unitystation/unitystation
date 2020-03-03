namespace Atmospherics
{
	public static class GasMixes
	{
		public static readonly GasMix Space;
		public static readonly GasMix Air;
		public static readonly GasMix Empty;

		static GasMixes()
		{
			Space = GasMix.FromTemperature(new float[Gas.Count], 2.7f);

			float[] gases = new float[Gas.Count];
			gases[Gas.Oxygen] = 16.628484400890768491815384755837f / 2 * 2.5f;
			gases[Gas.Nitrogen] = 66.513937603563073967261539023347f / 2 * 2.5f;

			Air = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);

			gases[Gas.Oxygen] = 0;
			gases[Gas.Nitrogen] = 0;
			Empty = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);
		}
	}
}