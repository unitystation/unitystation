using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Traitor")]
	public class Traitor : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		[SerializeField]
		private int initialTC = 20;

		public override ConnectedPlayer ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			ConnectedPlayer player = PlayerSpawn.ServerSpawnPlayer(spawnRequest).Player();

			AntagManager.TryInstallPDAUplink(player, initialTC);

			return player;
		}
	}
}
