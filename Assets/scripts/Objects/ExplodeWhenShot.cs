using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public class ExplodeWhenShot : NetworkBehaviour {
	string[] explosions = new[] {"Explosion1", "Explosion2"};

	void OnTriggerEnter2D(Collider2D coll){
		var bullet = coll.GetComponent<BulletBehaviour>();
		if (bullet != null) {
			if (isServer) {
				Explode(bullet.shooterName);
			}

			GoBoom();

			Destroy(bullet.gameObject);
		}
	}

	[Server]
	void Explode(string bulletOwnedBy) {
		// TODO: Damage people
		NetworkServer.Destroy(gameObject);
	}
		
	void GoBoom() {
		// Instantiate a clone of the source so that multiple explosions can play at the same time.
		var name = explosions[Random.Range(0, explosions.Length)];
		var source = SoundManager.Instance[name];
		Instantiate<AudioSource>(source, transform.position, Quaternion.identity).Play();

		var parent = Resources.Load<GameObject>("effects/FireRing");
		Instantiate(parent, transform.position, Quaternion.identity);
	}
}
