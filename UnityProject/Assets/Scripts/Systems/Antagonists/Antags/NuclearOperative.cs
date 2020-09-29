using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/NuclearOperative")]
	public class NuclearOperative : Antagonist
	{
		[Tooltip("For use in Syndicate Uplinks")]
		public int initialTC = 20;

		// add any NuclearOperative specific logic here
		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			//spawn as a nuke op regardless of the requested occupation
			var newPlayer = PlayerSpawn.ServerSpawnPlayer(spawnRequest.JoinedViewer, AntagOccupation,
				spawnRequest.CharacterSettings);

			//send the code:
			//Check to see if there is a nuke and communicate the nuke code:
			Nuke nuke = Object.FindObjectOfType<Nuke>();
			if (nuke != null)
			{
				UpdateChatMessage.Send(newPlayer, ChatChannel.Syndicate, ChatModifier.None,
					"We have intercepted the code for the nuclear weapon: " + nuke.NukeCode);
			}

			return newPlayer;
		}
	}
}
