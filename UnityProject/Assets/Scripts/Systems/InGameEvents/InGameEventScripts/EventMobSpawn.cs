using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using Managers;
using Strings;
using NaughtyAttributes;
using Systems.Spawns;

namespace InGameEvents
{
	public class EventMobSpawn : EventScriptBase
	{
		[Tooltip("Random mobs from this list will be spawned.")]
		[SerializeField, BoxGroup("References")]
		private GameObject[] mobPrefabs = default;

		[Tooltip("If the chance for a rare mob succeeds, a random rare mob from this list will be spawned .")]
		[SerializeField, BoxGroup("References")]
		private GameObject[] rareMobPrefabs = default;

		[Tooltip("Which spawn point category these mobs should use.")]
		[SerializeField, BoxGroup("Settings")]
		private SpawnPointCategory spawnPointCategory = default;

		[Tooltip("How many mobs should be spawned in total - will be randomly picked within the range.")]
		[SerializeField, BoxGroup("Settings"), MinMaxSlider(1, 100)]
		private Vector2 spawnCount = new Vector2(5, 15);

		[Tooltip("The chance (in percent) for a rare mob to spawn instead of a normal one.")]
		[SerializeField, BoxGroup("Settings"), Range(0, 100)]
		private int rareMobChance = 5;

		private List<Transform> spawnPoints;

		private bool SetSpawnPoints()
		{
			if (spawnPoints == null || spawnPoints.Count < 1)
			{
				spawnPoints = SpawnPoint.GetPointsForCategory(spawnPointCategory).ToList();
			}

			if (spawnPoints.Count < 1)
			{
				Loggy.LogError($"No spawn points found for {spawnPointCategory} in " +
					$"{SubSceneManager.ServerChosenMainStation}! Cannot start {this}.", Category.Event);
				return false;
			}

			return true;
		}

		protected virtual string GenerateMessage()
		{
			return "Unknown biological entities have been detected near the station, please stand-by.";
		}

		public override void OnEventStart()
		{
			if (AnnounceEvent)
			{
				CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, GenerateMessage(), CentComm.UpdateSound.Alert);
			}

			if (FakeEvent || SetSpawnPoints() == false) return;

			base.OnEventStart();
		}

		public override void OnEventStartTimed()
		{
			for (int i = 0; i < Random.Range(spawnCount.x, spawnCount.y); i++)
			{
				if (rareMobPrefabs.Length >= 1 && DMMath.Prob(rareMobChance))
				{
					Spawn.ServerPrefab(rareMobPrefabs.PickRandom(), spawnPoints.PickRandom().position);
				}
				else
				{
					Spawn.ServerPrefab(mobPrefabs.PickRandom(), spawnPoints.PickRandom().position);
				}
			}
		}
	}
}
