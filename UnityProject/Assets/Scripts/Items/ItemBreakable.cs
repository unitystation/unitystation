﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBreakable : MonoBehaviour
{
	private Integrity integrity;

	public float damageOnHit;

	public int integrityHealth;

	public GameObject brokenItem;

	public string soundOnBreak;

	// Start is called before the first frame update
	void Awake()
	{
		integrity = GetComponent<Integrity>();

		integrity.OnApllyDamage.AddListener(OnDamageReceived);
	}

	public void AddDamage()
	{
		integrity.ApplyDamage(damageOnHit, AttackType.Melee, DamageType.Brute);
		if (integrity.integrity <= integrityHealth)
		{
			ChangeState();
		}
	}
	private void ChangeState()
	{
		SoundManager.PlayNetworkedAtPos(soundOnBreak, gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
		Spawn.ServerPrefab(brokenItem, gameObject.AssumedWorldPosServer());
		Despawn.ServerSingle(gameObject);
	}

	private void OnDamageReceived(DamageInfo arg0)
	{
		if (integrity.integrity <= integrityHealth)
		{
			ChangeState();
		}
	}
}