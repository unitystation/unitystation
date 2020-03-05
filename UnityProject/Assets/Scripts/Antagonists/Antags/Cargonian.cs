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
				"<color=red>As a member of the Cargonian Members Federation you have been ordered to help in the efforts to secede from the rest of the station.</color>");
			// spawn them normally, with their preferred occupation
			return newPlayer;
		}
	}
}