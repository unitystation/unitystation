using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour, IServerSpawn
{
	public GameObject Prefab;


	public void OnSpawnServer(SpawnInfo info)
	{
		_ = Spawn.ServerPrefab(Prefab, transform.position).GameObject;
	}

}
