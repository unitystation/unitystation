using JetBrains.Annotations;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Systems.Antagonists;

namespace US13.Systems.GameModes
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
