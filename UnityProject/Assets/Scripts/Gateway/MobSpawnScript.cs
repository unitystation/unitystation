using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MobSpawnScript : NetworkBehaviour
{
	public GameObject MobToSpawn;

	[Server]
	public void SpawnMob()
	{
		if (MobToSpawn == null) return;
		Spawn.ServerPrefab(MobToSpawn, gameObject.GetComponent<RegisterTile>().WorldPosition);
		Destroy(this.gameObject);
	}
}
