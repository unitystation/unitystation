using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Managers;
using Systems.Spawns;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ScriptableObjects.TimedGameEvents
{
	[CreateAssetMenu(fileName = "HalloweenGhostsTimedEvent", menuName = "ScriptableObjects/Events/TimedGameEvents/HalloweenGhosts")]
	public class HalloweenGhosts : TimedGameEventSO
	{
		[SerializeField] private List<GameObject> horrorsToSpawn;
		[SerializeField] private SpawnPointCategory spawnPointCategory = SpawnPointCategory.MaintSpawns;
		[SerializeField] private Vector2 randomSpawnCount = new Vector2(1, 5);
		private const float WAIT_TIME_BEFORE_HAUNTS = 320f;
		private const float CHANCE_FOR_UNINTENDED_AREA = 5f;
		private List<Transform> spawnPoints = new List<Transform>();

		private bool isRunning = false;

		public override IEnumerator EventStart()
		{
			if(SetSpawns() == false) yield break;
			if (horrorsToSpawn.Count <= 0)
			{
				Logger.LogError("[TimedEvent/Halloween/Ghosts] - No ghosts assigned to spawn them!!", Category.Event);
				yield break;
			}

			isRunning = true;

			while (isRunning)
			{
				yield return WaitFor.Seconds(WAIT_TIME_BEFORE_HAUNTS);
				if (PlayerList.Instance.ConnectionCount == 0) continue;
				SpawnGhosts();
				ChanceToSetUnintendedSpawnArea();
			}
		}

		public override IEnumerator OnRoundEnd()
		{
			//ends the while loop in StartEvent()
			isRunning = false;
			//Because this is a scriptable object, data carries over. so make sure to clear it.
			spawnPoints = null;
			yield return null;
		}

		private void ChanceToSetUnintendedSpawnArea()
		{
			if(DMMath.Prob(CHANCE_FOR_UNINTENDED_AREA) == false) return;
			Array catag = Enum.GetValues(typeof(SpawnPointCategory));
			var newSpawnPoint = (SpawnPointCategory)catag.GetValue(Random.Range(0, catag.Length));
			var previousPoint = spawnPointCategory;
			spawnPointCategory = newSpawnPoint;
			if(SetSpawns() == false) spawnPointCategory = previousPoint;
		}

		private void SpawnGhosts()
		{
			if (spawnPoints == null && isRunning)
			{
				if(SetSpawns() == false) return;
			}
			for (int i = 0; i < Random.Range(randomSpawnCount.x, randomSpawnCount.y); i++)
			{
				Spawn.ServerPrefab(horrorsToSpawn.PickRandom(), spawnPoints.PickRandom().position);
			}
		}

		private bool SetSpawns()
		{
			spawnPoints = SpawnPoint.GetPointsForCategory(spawnPointCategory).ToList();

			if (spawnPoints.Count >= 1) return true;
			Logger.LogError($"No spawn points found for {spawnPointCategory} in " +
			                $"{SubSceneManager.ServerChosenMainStation}! Cannot start {this}.", Category.Event);
			return false;

		}
	}
}