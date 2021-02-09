using UnityEngine;

namespace Systems.Atmospherics
{
	public static class GasMixes
	{
		public static readonly GasMix Space;
		public static readonly GasMix Air;
		public static readonly GasMix Empty;
		public static readonly GasMix EmptyTile;

		static GasMixes()
		{
			float[] gases = new float[Gas.Count];

			//Space

			gases[Gas.Plasma] = 0;
			gases[Gas.Oxygen] = 0.001f;
			gases[Gas.Nitrogen] = 0;
			gases[Gas.CarbonDioxide] = 0.01f;

			gases[Gas.NitrousOxide] = 0;
			gases[Gas.Hydrogen] = 0;
			gases[Gas.WaterVapor] = 0;
			gases[Gas.BZ] = 0;
			gases[Gas.Miasma] = 0;
			gases[Gas.Nitryl] = 0;
			gases[Gas.Tritium] = 0;
			gases[Gas.HyperNoblium] = 0;
			gases[Gas.Stimulum] = 0;
			gases[Gas.Pluoxium] = 0;
			gases[Gas.Freon] = 0;

			Space = GasMix.FromTemperature(gases, 2.7f);

			//Air

			gases[Gas.Plasma] = 0;
			gases[Gas.Oxygen] = 16.628484400890768491815384755837f / 2 * 2.5f;
			gases[Gas.Nitrogen] = 66.513937603563073967261539023347f / 2 * 2.5f;
			gases[Gas.CarbonDioxide] = 0;

			gases[Gas.NitrousOxide] = 0;
			gases[Gas.Hydrogen] = 0;
			gases[Gas.WaterVapor] = 0;
			gases[Gas.BZ] = 0;
			gases[Gas.Miasma] = 0;
			gases[Gas.Nitryl] = 0;
			gases[Gas.Tritium] = 0;
			gases[Gas.HyperNoblium] = 0;
			gases[Gas.Stimulum] = 0;
			gases[Gas.Pluoxium] = 0;
			gases[Gas.Freon] = 0;

			Air = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);

			//Empty

			gases[Gas.Plasma] = 0;
			gases[Gas.Oxygen] = 0;
			gases[Gas.Nitrogen] = 0;
			gases[Gas.CarbonDioxide] = 0;

			gases[Gas.NitrousOxide] = 0;
			gases[Gas.Hydrogen] = 0;
			gases[Gas.WaterVapor] = 0;
			gases[Gas.BZ] = 0;
			gases[Gas.Miasma] = 0;
			gases[Gas.Nitryl] = 0;
			gases[Gas.Tritium] = 0;
			gases[Gas.HyperNoblium] = 0;
			gases[Gas.Stimulum] = 0;
			gases[Gas.Pluoxium] = 0;
			gases[Gas.Freon] = 0;

			EmptyTile = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20);			//With volume
			Empty = GasMix.FromTemperature(gases, Reactions.KOffsetC + 20, 0);	//Without volume

		}
	}
}
