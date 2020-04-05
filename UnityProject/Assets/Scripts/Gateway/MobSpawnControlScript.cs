using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MobSpawnControlScript : NetworkBehaviour
{
	public List<GameObject> MobSpawners;

	[Server]
	public void SpawnMobs()
	{
		foreach (GameObject Spawner in MobSpawners)
		{
			if (Spawner != null)
			{
				Spawner.GetComponent<MobSpawnScript>().SpawnMob();
			}
		}
	}
}
