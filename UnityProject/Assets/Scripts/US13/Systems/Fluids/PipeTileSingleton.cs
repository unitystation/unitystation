using UnityEngine;
using US13.Objects.Pipes;
using US13.ScriptableObjects;

namespace US13.Systems.Fluids
{
	[CreateAssetMenu(fileName = "PipeTileSingleton", menuName = "Singleton/PipeTileSingleton")]
	public class PipeTileSingleton : SingletonScriptableObject<PipeTileSingleton>
	{
		public PipeTile StraightWaterPipe;
		public PipeTile BentWaterPipe;
	}
}
