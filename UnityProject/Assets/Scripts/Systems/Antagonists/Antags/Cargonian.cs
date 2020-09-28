using System.Collections.Generic;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Cargonian")]
	public class Cargonian : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			var newPlayer = PlayerSpawn.ServerSpawnPlayer(spawnRequest);
			UpdateChatMessage.Send(newPlayer, ChatChannel.System, ChatModifier.None,
				"<color=red>Something has awoken in you. You feel the urgent need to rebel alongside all your brothers in your deparment against this station.</color>");
			// spawn them normally, with their preferred occupation
			return newPlayer;
		}
	}
}