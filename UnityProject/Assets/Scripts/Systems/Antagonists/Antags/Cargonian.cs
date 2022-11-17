using System.Collections.Generic;
using UnityEngine;
using Messages.Server;
using Player;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Cargonian")]
	public class Cargonian : Antagonist
	{
		public override Mind ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.NewSpawnPlayerV2(spawnRequest.Player, spawnRequest.RequestedOccupation, spawnRequest.CharacterSettings);
		}

		public override void AfterSpawn(Mind player)
		{
			UpdateChatMessage.Send(player.gameObject, ChatChannel.System, ChatModifier.None,
				"<color=red>Something has awoken in you. You feel the urgent need to rebel " +
				"alongside all your brothers in your department against this station.</color>");
		}
	}
}
