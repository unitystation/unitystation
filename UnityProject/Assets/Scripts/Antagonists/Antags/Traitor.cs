using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Traitor")]
	public class Traitor : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}
	}
}