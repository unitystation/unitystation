using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ScriptableObjects;

public class SpawnMobArtifactEffect : ArtifactEffect
{
	public GameObjectList mobsToSpawn;

	public int maxMobs = 3;

	private HashSet<GameObject> spawnedMobs = new HashSet<GameObject>();

	private List<Vector3> adjacentVectors = new List<Vector3>()
	{
		Vector3.down,
		Vector3.up,
		Vector3.left,
		Vector3.right
	};

	public override void DoEffectTouch(HandApply touchSource)
	{
		base.DoEffectTouch(touchSource);
		TrySpawnMob();
	}

	public override void DoEffectAura()
	{
		base.DoEffectAura();
		TrySpawnMob();
	}

	private void TrySpawnMob()
	{
		var pos = gameObject.WorldPosServer();

		spawnedMobs.Remove(null);

		var mobs = spawnedMobs;

		foreach (var mob in mobs)
		{
			if (mob.TryGetComponent<LivingHealthBehaviour>(out var health) && health.IsDead)
			{
				spawnedMobs.Remove(mob);
			}
		}

		if (spawnedMobs.Count >= maxMobs)
		{
			return;
		}

		foreach (var vector in adjacentVectors)
		{
			if(MatrixManager.IsPassableAtAllMatricesOneTile((pos + vector).RoundToInt(), true))
			{
				var result = Spawn.ServerPrefab(mobsToSpawn.GetRandom(), pos + vector, transform.parent.transform);
				spawnedMobs.Add(result.GameObject);
				return;
			}
		}
	}
}
