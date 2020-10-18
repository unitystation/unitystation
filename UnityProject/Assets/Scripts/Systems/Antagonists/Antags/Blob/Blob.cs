using UnityEngine;
using Blob;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Blob")]
	public class Blob : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			var spawn = PlayerSpawn.ServerSpawnPlayer(spawnRequest);

			//Add blob player to game object
			spawn.AddComponent<BlobStarter>();

			return spawn;
		}
	}
}