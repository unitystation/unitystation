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
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}

		public override void AfterSpawn(ConnectedPlayer player)
		{
			//Add blob player to game object
			player.GameObject.AddComponent<BlobStarter>();
		}
	}
}
