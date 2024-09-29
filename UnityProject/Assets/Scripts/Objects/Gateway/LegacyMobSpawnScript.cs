using UnityEngine;
using Mirror;

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
