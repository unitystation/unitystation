﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BulletBehaviour : MonoBehaviour {

    private Rigidbody2D thisRigi;
	public string shooterName;
	public int damage = 25;
//	public BodyPartType BodyPartAim { get; private set; };


	public void Shoot(Vector2 dir, float angle, string controlledByPlayer){
		StartShoot(dir, angle, controlledByPlayer);
        OnShoot();
    }
		
	void StartShoot(Vector2 dir, float angle, string controlledByPlayer){
		shooterName = controlledByPlayer;
    	transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
		Vector3 startPos = new Vector3(dir.x, dir.y, transform.position.z);
		transform.position += startPos;
        thisRigi = GetComponent<Rigidbody2D>();
		thisRigi.AddForce(dir.normalized * 24f, ForceMode2D.Impulse);
    }

    public abstract void OnShoot();

    void OnCollisionEnter2D(Collision2D coll){
		PoolManager.PoolClientDestroy(gameObject);
	    
    }

	private void OnTriggerEnter2D(Collider2D coll)
	{
		var damageable =  coll.GetComponent<HealthBehaviour>();
		if ( damageable == null || 
		     damageable.IsDead || 
		     damageable.gameObject.name.Equals( shooterName ) ) return;
		//todo: determine body part
		damageable.ApplyDamage(shooterName, damage, DamageType.BRUTE, BodyPartType.CHEST);
//		Debug.LogFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", damageable.gameObject.name, damage);
		PoolManager.PoolClientDestroy(gameObject);
	}
}
