using Antagonists;
using JetBrains.Annotations;
using Player;
using UnityEngine;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/MapEditor")]
	public class MapEditor : GameMode
	{
		public override bool IsPossible()
		{
			return true;
		}

		protected override Antagonist HandleRatioAndPickAntagonist(PlayerInfo PlayerInfo, [CanBeNull] PlayerSpawnRequest spawnRequest, int NumberChosenAlready)
		{
			return null;
		}


	}
}