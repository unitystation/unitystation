using System.Collections;
using System.Collections.Generic;
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

	void Start()
	{
		playerMask = LayerMask.GetMask("Players");
		obstacleMask = LayerMask.GetMask("Walls", "Door Closed");
	}

	void OnTriggerEnter2D(Collider2D coll)
	{
		var bullet = coll.GetComponent<BulletBehaviour>();
		if (bullet != null) {
			if (isServer) {
				Explode(bullet.shooterName);
			}

			GoBoom();

			Destroy(bullet.gameObject);
		}
	}

	#if !ENABLE_PLAYMODE_TESTS_RUNNER
	[Server]
	#endif
	public void Explode(string bulletOwnedBy)
	{
		var pos = (Vector2)transform.position;
		var length = Physics2D.OverlapCircleNonAlloc(pos, radius, colliders, playerMask);

		NetworkServer.Destroy(gameObject);

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
	}

	void GoBoom()
	{
		// Instantiate a clone of the source so that multiple explosions can play at the same time.
		var name = explosions[Random.Range(0, explosions.Length)];
		var source = SoundManager.Instance[name];
		if (source != null) {
			Instantiate<AudioSource>(source, transform.position, Quaternion.identity).Play();
		}

		var parent = Resources.Load<GameObject>("effects/FireRing");
		Instantiate(parent, transform.position, Quaternion.identity);
	}

	internal virtual void HurtPeople(Living living, string damagedBy, int damage)
	{
		living.RpcReceiveDamage(damagedBy, damage);
	}
}
