using Antagonists;
using JetBrains.Annotations;
using UnityEngine;
using Player;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Extended")]
	public class Extended : GameMode
	{
		protected override Antagonist HandleRatioAndPickAntagonist(PlayerInfo PlayerInfo, [CanBeNull] PlayerSpawnRequest spawnRequest, int NumberChosenAlready)
		{
			//no antags
			return null;
		}
	}
}
