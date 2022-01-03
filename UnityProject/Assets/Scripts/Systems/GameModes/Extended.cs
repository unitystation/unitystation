using UnityEngine;
using Player;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Extended")]
	public class Extended : GameMode
	{
		protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
		{
			//no antags
			return false;
		}
	}
}
