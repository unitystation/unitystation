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
			//Space
			var spaceData = new GasData();
			Space = GasMix.FromTemperature(spaceData, 2.7f);

			//Air
			var airData = new GasData();
			airData.SetMoles(GasType.Oxygen, 16.628484400890768491815384755837f / 2 * 2.5f);
			airData.SetMoles(GasType.Nitrogen, 66.513937603563073967261539023347f / 2 * 2.5f);
			Air = GasMix.FromTemperature(airData, Reactions.KOffsetC + 20);

			//Empty
			var emptyData = new GasData();
			EmptyTile = GasMix.FromTemperature(emptyData, Reactions.KOffsetC + 20);			//With volume
			Empty = GasMix.FromTemperature(emptyData, Reactions.KOffsetC + 20, 0);	//Without volume
		}
	}
}
