using System.Collections.Concurrent;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Health.Sickness
{
	/// <summary>
	/// Sickness subsystem manager
	/// </summary>
	public class SicknessManager : SingletonManager<SicknessManager>
	{
		public List<Sickness> Sicknesses;

		public List<MobSickness> sickPlayers = new List<MobSickness>();

		[SerializeField]
		private GameObject contagionPrefab = null;

		public static void SpawnContagionSpot(Sickness sicknessToSpawn, Vector3 position)
		{
			SpawnResult spawnResult = Spawn.ServerPrefab(Instance.contagionPrefab, position, null, null, 1, null, true);
			SpawnResult sickNessResult = Spawn.ServerPrefab(sicknessToSpawn.gameObject, position);

			if (spawnResult.Successful && sickNessResult.Successful && spawnResult.GameObject.TryGetComponent<Contagion>(out var contagion))
			{
				contagion.Setup(sickNessResult.GameObject);
			}
		}

		// Add this player as a sick player
		public void RegisterSickPlayer(MobSickness mobSickness)
		{
			lock (sickPlayers)
			{
				if (!sickPlayers.Contains(mobSickness))
					sickPlayers.Add(mobSickness);
			}
		}

		// Remove this player from the sick players
		public void UnregisterHealedPlayer(MobSickness mobSickness)
		{
			lock (sickPlayers)
			{
				if (!sickPlayers.Contains(mobSickness))
					sickPlayers.Remove(mobSickness);
			}
		}

		public void HealEveryone()
		{
			if(CustomNetworkManager.IsServer == false) return;
			foreach (var player in sickPlayers)
			{
				foreach (var sickness in player.MobHealth.mobSickness.sicknessAfflictions)
				{
					sickness.Heal();
				}
			}
		}
	}
}