using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MobSpawnControlScript : NetworkBehaviour
{
	public List<GameObject> MobSpawners;

	public bool DetectViaMatrix;

	private bool SpawnedMobs;

	private float timeElapsedServer = 0;

	[Server]
	public void SpawnMobs()
	{
		if (SpawnedMobs) return;

		SpawnedMobs = true;

		foreach (GameObject Spawner in MobSpawners)
		{
			if (Spawner != null)
			{
				Spawner.GetComponent<MobSpawnScript>().SpawnMob();
			}
		}
	}

	protected virtual void UpdateMe()
	{
		if (isServer)
		{
			timeElapsedServer += Time.deltaTime;
			if (timeElapsedServer > 1f && !SpawnedMobs)
			{
				DetectPlayer();
				timeElapsedServer = 0;
			}
		}
	}

	private void OnEnable()
	{
		if (!DetectViaMatrix) return;
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}
	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	[Server]
	private void DetectPlayer()
	{
		foreach (var player in PlayerList.Instance.InGamePlayers)
		{
			var playerScript = player.Script;

			if (playerScript == null) return;

			if (playerScript.registerTile.Matrix == gameObject.GetComponent<RegisterObject>().Matrix)
			{
				SpawnMobs();
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				return;
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
