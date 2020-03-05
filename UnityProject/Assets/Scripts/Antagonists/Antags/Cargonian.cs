using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Cargonian")]
	public class Cargonian : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}
	}
}