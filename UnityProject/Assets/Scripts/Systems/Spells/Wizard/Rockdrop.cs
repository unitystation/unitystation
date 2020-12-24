using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Systems.Explosions;

namespace Systems.Spells.Wizard
{
	// TODO: no sounds; wait for addressable sounds to work.

	/// <summary>
	/// A wizard spell that summons a bunch of rocks at the target position.
	/// </summary>
	public class Rockdrop : Spell
	{
		[SerializeField, BoxGroup("References")]
		private GameObject portalPrefab = default;
		[SerializeField, BoxGroup("References")]
		private GameObject mainRockPrefab = default;
		[SerializeField, BoxGroup("References")]
		private GameObject smallRockPrefab = default;

		[SerializeField, BoxGroup("Settings"), Range(0, 10)]
		private int smallRocksToSpawn = 5;
		[SerializeField, BoxGroup("Settings"), Range(0, 1)]
		private float timeBetweenRocks = 0.1f;

		private readonly List<Vector3Int> usedPositions = new List<Vector3Int>();

		public override bool CastSpellServer(ConnectedPlayer caster, Vector3 clickPosition)
		{
			PortalSpawnInfo settings = PortalSpawnInfo.DefaultSettings();
			settings.EntityRotate = false; // A rotated large rock doesn't look great on landing.

			var rockPortalSpawn = new SpawnByPortal(mainRockPrefab, portalPrefab, clickPosition, settings);
			rockPortalSpawn.OnObjectSpawned += (GameObject mainRock) =>
			{
				mainRock.GetComponent<RegisterObject>().Passable = true; // Passable until it lands.
			};
			rockPortalSpawn.OnObjectLanded += (GameObject mainRock) =>
			{
				OnRockLanded(mainRock, 120);
				mainRock.GetComponent<RegisterObject>().Passable = false;
			};

			StartCoroutine(SpawnSmallRocks(clickPosition));

			return true;
		}

		private IEnumerator SpawnSmallRocks(Vector3 centrepoint)
		{
			for (int i = 0; i < smallRocksToSpawn; i++)
			{
				yield return WaitFor.Seconds(timeBetweenRocks);

				new SpawnByPortal(smallRockPrefab, portalPrefab, TryGetUniqueRandomPosition(centrepoint.CutToInt()))
						.OnObjectLanded += (GameObject smallRock) =>
				{
					OnRockLanded(smallRock, 60);
				};
			}
		}

		private void OnRockLanded(GameObject rock, float damage)
		{
			var landingPosition = rock.RegisterTile().WorldPositionServer;
			var matrixInfo = MatrixManager.AtPoint(landingPosition, true);

			Explosion.StartExplosion(landingPosition, damage, matrixInfo.Matrix);
			ExplosionUtils.PlaySoundAndShake(landingPosition, 16, 4);
		}

		private Vector3Int TryGetUniqueRandomPosition(Vector3Int centrepoint)
		{
			Vector3Int randomPosition = Vector3Int.zero;

			// Quick solution to try get unique positions.
			for (int i = 0; i < 5; i++)
			{
				randomPosition = RandomUtils.RandomAnnulusPoint(1, 2).CutToInt();
				if (usedPositions.Contains(randomPosition) == false) break;
			}

			usedPositions.Add(randomPosition);
			return centrepoint + randomPosition;
		}
	}
}
