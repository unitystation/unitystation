using ScriptableObjects.Atmospherics;
using UnityEngine;

namespace Systems.Atmospherics
{
	public static class GasMixes
	{
		public static GasMix Space => GasMixesSingleton.Instance.space.GasMix;
		public static GasMix Air => GasMixesSingleton.Instance.air.GasMix;
		public static GasMix Empty => GasMixesSingleton.Instance.empty.GasMix;
		public static GasMix EmptyTile => GasMixesSingleton.Instance.emptyTile.GasMix;
	}
}
