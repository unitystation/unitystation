using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
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
		[SerializeField] private float spawnChance = 35f;
		[SerializeField] private Vector2Int randomSpawnCount = new Vector2Int(1, 5);
		private const float WAIT_TIME_BEFORE_HAUNTS = 865f;
		private const float CHANCE_FOR_UNINTENDED_AREA = 5f;
		private List<Transform> spawnPoints = new List<Transform>();

		private bool isRunning = false;

		public override IEnumerator EventStart()
		{
			if(SetSpawns() == false) yield break;
			if (horrorsToSpawn.Count <= 0)
			{
				Loggy.LogError("[TimedEvent/Halloween/Ghosts] - No ghosts assigned to spawn them!!", Category.Event);
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

		public override void Clean()
		{
			//ends the while loop in StartEvent()
			isRunning = false;
			//Because this is a scriptable object, data carries over. so make sure to clear it.
			spawnPoints.Clear();
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
			if (DMMath.Prob(spawnChance) == false) return;
			var randomCount = (int)Random.Range(randomSpawnCount.x, randomSpawnCount.y);
			for (int i = 0; i < randomCount; i++)
			{
				Spawn.ServerPrefab(horrorsToSpawn.PickRandom(), spawnPoints.PickRandom().position);
			}
		}

		private bool SetSpawns()
		{
			spawnPoints = SpawnPoint.GetPointsForCategory(spawnPointCategory).ToList();

			if (spawnPoints.Count >= 1) return true;
			Loggy.LogError($"No spawn points found for {spawnPointCategory} in " +
			                $"{SubSceneManager.ServerChosenMainStation}! Cannot start {this}.", Category.Event);
			return false;

		}
	}
}