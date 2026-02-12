using JetBrains.Annotations;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Systems.Antagonists;

namespace US13.Systems.GameModes
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