using System.Collections.Generic;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Core.Utils;
using US13.HealthV2;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Systems.Explosions;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Systems.Spells.Wizard
{
	public class TheTraps : Spell
	{
		public List<GameObject> Trapsprefabs;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition, BodyPartType targetZone)
		{
			var position = caster.Script.gameObject.AssumedWorldPosServer();
			var matrix = MatrixManager.AtPoint(position, true);
			//Explosion.StartExplosion(position.RoundToInt(), 1000f, ExplosionTypes.NodeTypes[ExplosionTypes.ExplosionType.DarkMatter]);

			//return true;

			int prefabCount = RNG.GetRandomNumber(2, 4);
			const int range = 2;
			const int maxFailedAttempts = 25;

			var localPosition = position.ToLocalInt(matrix);

			int spawned = 0;
			int failedAttempts = 0;

			while (spawned < prefabCount && failedAttempts < maxFailedAttempts)
			{
				Vector3Int chosenPosition = localPosition + new Vector3Int(
					RNG.GetRandomNumber(-range, range + 1),
					RNG.GetRandomNumber(-range, range + 1),
					0
				);

				// Don't spawn directly on the caster.
				if (chosenPosition == localPosition)
				{
					failedAttempts++;
					continue;
				}

				if (IsSpotFree(chosenPosition, matrix) == false)
				{
					failedAttempts++;
					continue;
				}

				var trap = Spawn.ServerPrefab(
					Trapsprefabs.PickRandom(),
					chosenPosition.ToWorld(matrix)
				);

				trap.GameObject.GetComponent<MagicTrap>().ToIgnore = caster.Script.GameObject;

				spawned++;
			}

			return true;
		}

		private bool IsSpotFree(Vector3Int position, MatrixInfo matrix)
		{
			if (matrix.MetaTileMap.HasTile(position) == false)
				return false;

			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is BasicTile;
		}
	}
}