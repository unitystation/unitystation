using Systems.Atmospherics;
using UnityEngine;

namespace ScriptableObjects.Atmospherics
{

	[CreateAssetMenu(fileName = "GasMixesSingleton", menuName = "Singleton/GasMixesSingleton")]
	public class GasMixesSingleton : SingletonScriptableObject<GasMixesSingleton>
	{
		public GasMixesSO space;
		public GasMixesSO air;
		public GasMixesSO empty;
		public GasMixesSO emptyTile;
	}
}
