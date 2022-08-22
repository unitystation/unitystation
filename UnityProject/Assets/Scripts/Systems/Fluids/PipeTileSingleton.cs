using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;


namespace Objects.Atmospherics
{
	[CreateAssetMenu(fileName = "PipeTileSingleton", menuName = "Singleton/PipeTileSingleton")]
	public class PipeTileSingleton : SingletonScriptableObject<PipeTileSingleton>
	{
		public PipeTile StraightWaterPipe;
		public PipeTile BentWaterPipe;
	}
}
