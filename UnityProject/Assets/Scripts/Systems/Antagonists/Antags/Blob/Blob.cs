using UnityEngine;
using Blob;
using Player;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Blob")]
	public class Blob : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}

		public override void AfterSpawn(PlayerInfo player)
		{
			//Add blob player to game object
			player.GameObject.AddComponent<BlobStarter>();
		}
	}
}
