using Player;
using UnityEngine;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/MapEditor")]
	public class MapEditor : GameMode
	{
		public override bool IsPossible()
		{
			return true;
		}

		protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
		{
			return false;
		}


	}
}