using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using SecureStuff;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MobSpawnControlScript : NetworkBehaviour
{
	public List<LegacyMobSpawnScript> MobSpawners = new List<LegacyMobSpawnScript>();

	public List<PlayerBlueprint> MobSpawnersNew = new List<PlayerBlueprint>();

	public bool DetectViaMatrix;

	private bool SpawnedMobs;

	private const float PlayerCheckTime = 1f;

	[Server, VVNote(VVHighlight.SafeToModify100)]
	public void SpawnMobs()
	{
		if (SpawnedMobs) return;
		SpawnedMobs = true;

		foreach (var Spawner in MobSpawners)
		{
			if (Spawner != null)
			{
				Spawner.SpawnMob();
			}
		}

		foreach (var Spawner in MobSpawnersNew)
		{
			if (Spawner != null)
			{
				Spawner.Spawn();
			}
		}
	}

	[ContextMenu("Rebuild mob spawner list"), VVNote(VVHighlight.SafeToModify100), NaughtyAttributes.Button]
	void RebuildMobSpawnerList()
	{
		MobSpawners.Clear();
		MobSpawnersNew.Clear();
		foreach (Transform t in transform.parent)
		{
			var mobSpawner = t.GetComponent<LegacyMobSpawnScript>();
			if (mobSpawner != null)
			{
				MobSpawners.Add(mobSpawner);
			}

			var Blueprint = t.GetComponent<PlayerBlueprint>();
			if (Blueprint != null)
			{
				MobSpawnersNew.Add(Blueprint);
			}
		}


		#if UNITY_EDITOR
		EditorUtility.SetDirty(gameObject);
		#endif
	}

	protected virtual void UpdateMe()
	{
		DetectPlayer();
	}

	private void OnEnable()
	{
		if (!DetectViaMatrix) return;
		if (!CustomNetworkManager.IsServer) return;

		UpdateManager.Add(UpdateMe, PlayerCheckTime);
	}

	void OnDisable()
	{
		if (!CustomNetworkManager.IsServer) return;

		UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
	}

	[Server]
	private void DetectPlayer()
	{
		foreach (var player in PlayerList.Instance.InGamePlayers)
		{
			var script = player.Script;
			if (script == null) return;

			if (script.IsNormal && script.RegisterPlayer.Matrix == gameObject.GetComponent<RegisterObject>().Matrix)
			{
				SpawnMobs();
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
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
