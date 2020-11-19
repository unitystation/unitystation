using System.Collections;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Survivor")]
	public class Survivor : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}
	}
}
