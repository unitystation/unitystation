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

		private void SpawnContagionSpot(Sickness sicknessToSpawn, Vector3 position)
		{
			SpawnResult spawnResult = Spawn.ServerPrefab(contagionPrefab, position, null, null, 1, null, true);

			if (spawnResult.Successful && spawnResult.GameObject.TryGetComponent<Contagion>(out var contagion))
			{
				contagion.Sickness = sicknessToSpawn;
				contagion.Sickness.SetCure(contagion.Sickness.PossibleCures.PickRandom());
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
	}
}