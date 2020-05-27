using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	/// <summary>
	/// Used for getting pipe tiles
	/// </summary>
	[CreateAssetMenu(fileName = "PipeTileSingleton", menuName = "Singleton/PipeTileSingleton")]
	public class PipeTileSingleton : SingletonScriptableObject<PipeTileSingleton>
	{
		//Probably should be a list but this is easier
		//This is taking unmodified connections
		public PipeTile StraightWaterPipe;
		public PipeTile BentWaterPipe;

		public PipeTile GetTile(Connections connections, CorePipeType Category)
		{
			switch (Category)
			{
				case CorePipeType.WaterPipe:
					if (connections.Directions[(int) PipeDirection.North].Bool)
					{
						return (StraightWaterPipe);
					}
					else if (connections.Directions[(int) PipeDirection.East].Bool)
					{
						return (BentWaterPipe);
					}
					break;
			}
			Logger.Log("OH GOD Unknown tile!!!");
			return (null);

		}
	}
}