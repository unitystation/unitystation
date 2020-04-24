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

	void OnDrawGizmosSelected()
	{
		var sprite = GetComponentInChildren<SpriteRenderer>();
		if (sprite == null)
			return;

		//Highlighting all controlled lightSources
		Gizmos.color = new Color(0.5f, 0.5f, 1, 1);
		for (int i = 0; i < MobSpawners.Count; i++)
		{
			var mobSpawner = MobSpawners[i];
			if (mobSpawner == null) continue;
			Gizmos.DrawLine(sprite.transform.position, mobSpawner.transform.position);
			Gizmos.DrawSphere(mobSpawner.transform.position, 0.25f);
		}
	}
}
