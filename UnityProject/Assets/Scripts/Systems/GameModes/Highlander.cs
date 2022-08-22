using System;
using System.Collections.Generic;
using UnityEngine;
using Player;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Highlander")]
	public class Highlander : GameMode
	{
		protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
		{
			return true;
		}
	}
}