using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class ExplodeWhenShotTest {
	ExplodeWhenShot subject;

	[SetUp]
	public void SetUp() {
		var obj = new GameObject();
		obj.AddComponent<SoundManager>();
		subject = obj.AddComponent<ExplodeWhenShot>();
	}

	[UnityTest]
	public IEnumerator Should_Destroy_Bullet() {
		var bullet = new GameObject();
		var collider = bullet.AddComponent<BoxCollider2D>();
		bullet.AddComponent<Bullet_12mm>();

		subject.SendMessage("OnTriggerEnter2D", collider);

		yield return 0;

		Assert.That(bullet == null);
	}

	[UnityTest]
	public IEnumerator Should_Destroy_Object() {
		subject.Explode(null);

		yield return 0;

		Assert.That(subject == null);
	}
}
