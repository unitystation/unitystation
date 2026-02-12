using Mirror;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Tilemaps.Behaviours.Objects;

namespace US13.Objects.Gateway
{
	public class LegacyMobSpawnScript : NetworkBehaviour
	{
		public GameObject MobToSpawn;

		[Server]
		public void SpawnMob()
		{
			if (MobToSpawn == null) return;

			var spawnResult = Spawn.ServerPrefab(MobToSpawn, gameObject.GetComponent<RegisterTile>().WorldPosition, transform.parent);
		}
	}
}
