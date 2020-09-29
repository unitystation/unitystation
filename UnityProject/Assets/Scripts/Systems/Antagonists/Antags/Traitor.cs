using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Traitor")]
	public class Traitor : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		public int initialTC = 20;

		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}
	}
}
