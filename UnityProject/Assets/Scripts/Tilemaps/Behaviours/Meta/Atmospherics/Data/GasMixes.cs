using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class GasMixes
	{
		/// <summary>
		/// Do not change anything directly, copy this gas mix using gasMix.Copy() or GasMix.NewGasMix()
		/// As this is an SO and will change the original values
		/// </summary>
		public static GasMix BaseSpaceMix => GasMixesSingleton.Instance.space.BaseGasMix;

		/// <summary>
		/// Do not change anything directly, copy this gas mix using gasMix.Copy() or GasMix.NewGasMix()
		/// As this is an SO and will change the original values
		/// </summary>
		public static GasMix BaseAirMix => GasMixesSingleton.Instance.air.BaseGasMix;

		/// <summary>
		/// Do not change anything directly, copy this gas mix using gasMix.Copy() or GasMix.NewGasMix()
		/// As this is an SO and will change the original values
		/// </summary>
		public static GasMix BaseEmptyMix => GasMixesSingleton.Instance.empty.BaseGasMix;

		/// <summary>
		/// Do not change anything directly, copy this gas mix using gasMix.Copy() or GasMix.NewGasMix()
		/// As this is an SO and will change the original values
		/// </summary>
		public static GasMix BaseEmptyTileMix => GasMixesSingleton.Instance.emptyTile.BaseGasMix;
	}
}
