using JetBrains.Annotations;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Systems.Antagonists;
using Util;

namespace US13.Systems.GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Highlander")]
	public class Highlander : GameMode
	{
		protected override Antagonist HandleRatioAndPickAntagonist(PlayerInfo PlayerInfo, [CanBeNull] PlayerSpawnRequest spawnRequest, int NumberChosenAlready)
		{
			return PossibleAntags.PickRandom();
		}
	}
}