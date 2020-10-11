using UnityEngine;

namespace Antagonists
{
	[CreateAssetMenu(menuName="ScriptableObjects/Antagonist/Wizard")]
	public class Wizard : Antagonist
	{
		[Tooltip("How many random spells the wizard should start with.")]
		[SerializeField]
		private int startingSpellCount = 0;

		public int StartingSpellCount => startingSpellCount;

		public override GameObject ServerSpawn(PlayerSpawnRequest spawnRequest)
		{
			var newPlayer = PlayerSpawn.ServerSpawnPlayer(spawnRequest.JoinedViewer, AntagOccupation,
				spawnRequest.CharacterSettings);

			return newPlayer;
		}
	}
}
