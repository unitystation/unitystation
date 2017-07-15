﻿using System.Collections;
using System.Collections.Generic;
using Light2D;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public class ExplodeWhenShot : NetworkBehaviour
{
	public int damage = 150;
	public float radius = 3f;

	const int MAX_TARGETS = 44;

	readonly string[] explosions = new[] { "Explosion1", "Explosion2" };
	readonly Collider2D[] colliders = new Collider2D[MAX_TARGETS];

	int playerMask;
	int obstacleMask;
	private bool hasExploded = false;

	private GameObject lightFxInstance;
	private LightSprite lightSprite;
	public SpriteRenderer spriteRend;
	
	void Start()
	{
		playerMask = LayerMask.GetMask("Players");
		obstacleMask = LayerMask.GetMask("Walls", "Door Closed");
	}

	void OnTriggerEnter2D(Collider2D coll)
	{
		if (hasExploded)
			return;
		
		var bullet = coll.GetComponent<BulletBehaviour>();
		if (bullet != null) {
			if (isServer ) {
				Explode(bullet.shooterName);
			}
			hasExploded = true;
			GoBoom();
			PoolManager.PoolClientDestroy(bullet.gameObject);
		}
	}
		
	#if !ENABLE_PLAYMODE_TESTS_RUNNER
	[Server]
	#endif
	public void Explode(string bulletOwnedBy)
	{
		var pos = (Vector2)transform.position;
		var length = Physics2D.OverlapCircleNonAlloc(pos, radius, colliders, playerMask);

		for (int i = 0; i < length; i++) {
			var collider = colliders[i];
			var living = collider.gameObject.GetComponent<Living>();
			if (living != null) {
				var livingPos = (Vector2)living.transform.position;
				var distance = Vector3.Distance(pos, livingPos);

				var hit = Physics2D.Raycast(pos, livingPos - pos, distance, obstacleMask);
				if (hit.collider == null) {
					var effect = 1 - ((distance * distance) / (radius * radius));
					var actualDamage = (int)(damage * effect);
					HurtPeople(living, bulletOwnedBy, actualDamage);
				}
			}
		}
		NetworkServer.Destroy(gameObject);
	}
		
	void GoBoom()
	{
		if(spriteRend.isVisible)
		Camera2DFollow.followControl.Shake(0.4f, 0.4f);
		// Instantiate a clone of the source so that multiple explosions can play at the same time.
		var name = explosions[Random.Range(0, explosions.Length)];
		var source = SoundManager.Instance[name];
		if (source != null) {
			Instantiate<AudioSource>(source, transform.position, Quaternion.identity).Play();
		}

		var fireRing = Resources.Load<GameObject>("effects/FireRing");
		Instantiate(fireRing, transform.position, Quaternion.identity);
		
		var lightFx = Resources.Load<GameObject>("lighting/BoomLight");
		lightFxInstance = Instantiate(lightFx, transform.position, Quaternion.identity);
		lightSprite = lightFxInstance.GetComponentInChildren<LightSprite>();
		lightSprite.fadeFX(1f);
		SetFire();
	}

	void SetFire(){
		int maxNumOfFire = 4;
		int cLength = 3;
		int rHeight = 3;
		var pos = (Vector2)transform.position;
		ItemFactory.Instance.SpawnFileTile(Random.Range(0.4f, 1f), pos);
		pos.x -= 1f;
		pos.y += 1f;

		for (int i = 0; i < cLength; i++) {
		
			for (int j = 0; j < rHeight; j++) {
				if (j == 0 && i == 0 || j == 2 && i == 0 || j == 2 && i == 2)
					continue;
				
					Vector2 checkPos = new Vector2(pos.x + (float)i, pos.y - (float)j);
					if (Matrix.Matrix.At(checkPos).IsPassable() || Matrix.Matrix.At(checkPos).IsPlayer()) {
					ItemFactory.Instance.SpawnFileTile(Random.Range(0.4f, 1f), checkPos);
						maxNumOfFire--;
					}
					if (maxNumOfFire <= 0) {
						break;
					}
				}
		}
	}
	

	internal virtual void HurtPeople(Living living, string damagedBy, int damage)
	{
		living.RpcReceiveDamage(damagedBy, damage);
	}
}
