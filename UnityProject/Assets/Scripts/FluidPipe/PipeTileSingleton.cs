using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	/// <summary>
	/// Used for Loading up them
	/// </summary>
	[CreateAssetMenu(fileName = "PipeTileSingleton", menuName = "Singleton/PipeTileSingleton")]
	public class PipeTileSingleton : SingletonScriptableObject<PipeTileSingleton>
	{
		public PipeTile StraightWaterPipe;
		public PipeTile BentWaterPipe;
	}
}