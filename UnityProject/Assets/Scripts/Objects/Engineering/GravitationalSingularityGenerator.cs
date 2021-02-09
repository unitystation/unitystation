using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;

public class GravitationalSingularityGenerator : MonoBehaviour, IOnHitDetect
{
	[SerializeField]
	private GameObject singularityPrefab = null;

	private RegisterTile registerTile;

	private float damageTaken;

	[SerializeField]
	private float radDamageNeedToSpawn = 200f;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public void OnHitDetect(DamageData damageData)
	{
		if(damageData.AttackType != AttackType.Rad) return;

		damageTaken += damageData.Damage;

		if (damageTaken >= radDamageNeedToSpawn)
		{
			SpawnSingularity();
		}
	}

	private void SpawnSingularity()
	{
		Spawn.ServerPrefab(singularityPrefab, registerTile.WorldPositionServer, gameObject.transform.parent);
		Despawn.ServerSingle(gameObject);
	}
}
