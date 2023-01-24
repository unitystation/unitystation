using Messages.Server;
using Player;
using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName = "ScriptableObjects/Antagonist/Changelings")]
	public class Changeling : Antagonist
	{
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			// spawn them normally, with their preferred occupation
			return PlayerSpawn.ServerSpawnPlayer(spawnRequest);
		}

		public override void AfterSpawn(PlayerInfo player)
		{
			UpdateChatMessage.Send(player.GameObject, ChatChannel.System, ChatModifier.None,
				"<color=red>You are a Changeling. A freak of nature who can change their form from aquired DNA. You have been contracted to fulfill your objectives.</color>");
		}
	}
}
