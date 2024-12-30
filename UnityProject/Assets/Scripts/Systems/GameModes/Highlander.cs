using System;
using System.Collections.Generic;
using Antagonists;
using JetBrains.Annotations;
using UnityEngine;
using Player;

namespace GameModes
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