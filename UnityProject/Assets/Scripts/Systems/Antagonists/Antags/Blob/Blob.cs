using UnityEngine;
using Blob;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Blob")]
	public class Blob : Antagonist
	{
		public override ConnectedPlayer ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			var spawn = PlayerSpawn.ServerSpawnPlayer(spawnRequest).Player();

			//Add blob player to game object
			spawn.GameObject.AddComponent<BlobStarter>();

			return spawn;
		}
	}
}
