using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Wizard")]
	public class Wizard : Antagonist
	{
		[Tooltip("How many random spells the wizard should start with.")]
		public int startingSpellCount = 1;

		[Tooltip("For use in Syndicate Uplinks")]
		public int startingMagicPoints = 3;


		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			var newPlayer = PlayerSpawn.ServerSpawnPlayer(spawnRequest.JoinedViewer, AntagOccupation,
				spawnRequest.CharacterSettings);

			return newPlayer;
		}
	}
}
