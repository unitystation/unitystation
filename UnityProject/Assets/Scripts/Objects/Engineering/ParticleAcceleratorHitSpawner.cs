using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;

public class ParticleAcceleratorHitSpawner : MonoBehaviour, IOnHitDetect
{
	[SerializeField]
	private GameObject prefabToSpawn = null;

	private RegisterTile registerTile;

	private float damageTaken;

	[SerializeField]
	private float radDamageNeedToSpawn = 200f;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public void OnHitDetect(OnHitDetectData data)
	{
		if(data.DamageData.AttackType != AttackType.Rad) return;

		damageTaken += data.DamageData.Damage;

		if (damageTaken >= radDamageNeedToSpawn)
		{
			SpawnPrefab();
		}
	}

	private void SpawnPrefab()
	{
		Spawn.ServerPrefab(prefabToSpawn, registerTile.WorldPositionServer, transform.parent.transform);
		Despawn.ServerSingle(gameObject);
	}
}
