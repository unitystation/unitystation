using UnityEngine;

namespace US13.ScriptableObjects.Atmospherics
{
	[CreateAssetMenu(fileName = "GasMixesSingleton", menuName = "Singleton/Atmos/GasMixesSingleton")]
	public class GasMixesSingleton : SingletonScriptableObject<GasMixesSingleton>
	{
		public GasMixesSO space;
		public GasMixesSO air;
		public GasMixesSO empty;
		public GasMixesSO emptyTile;
	}
}
