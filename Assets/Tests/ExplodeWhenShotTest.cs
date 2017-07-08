using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System;
using System.Collections;

public class ExplodeWhenShotTest
{
	MockExplodeWhenShot subject;

	[SetUp]
	public void SetUp()
	{
		var obj = new GameObject();
		obj.AddComponent<SoundManager>();
		subject = obj.AddComponent<MockExplodeWhenShot>();
	}

	[UnityTest]
	public IEnumerator Should_Destroy_Bullet()
	{
		var bullet = new GameObject();
		var collider = bullet.AddComponent<BoxCollider2D>();
		bullet.AddComponent<Bullet_12mm>();

		try {
			subject.SendMessage("OnTriggerEnter2D", collider);

			yield return 0;

			Assert.That(bullet == null);
		} finally {
			UnityEngine.Object.Destroy(bullet);
		}
	}

	[UnityTest]
	public IEnumerator Should_Destroy_Object()
	{
		subject.Explode(null);

		yield return 0;

		Assert.That(subject == null);
	}

	[Test]
	public void Should_Damage_Nearby_Player()
	{
		var player = new GameObject();

		player.AddComponent<BoxCollider2D>();
		var living = player.AddComponent<Living>();
		player.layer = LayerMask.NameToLayer("Players");

		Living damaged = null;
		subject.callback = t => damaged = t;

		try {
			subject.Explode(null);

			Assert.That(living == damaged);
		} finally {
			UnityEngine.Object.Destroy(player);
		}
	}

	[Test]
	public void Should_Not_Damage_Player_Through_Wall()
	{
		var player = new GameObject();
		player.AddComponent<BoxCollider2D>();
		player.AddComponent<Living>();
		player.layer = LayerMask.NameToLayer("Players");
		player.transform.position = new Vector3(2, 0);

		var wall = new GameObject();
		wall.AddComponent<BoxCollider2D>();
		wall.layer = LayerMask.NameToLayer("Walls");
		wall.transform.position = new Vector3(1, 0);

		Living damaged = null;
		subject.callback = t => damaged = t;

		subject.Explode(null);

		Assert.That(damaged == null);
	}

	// TODO: Add a unity-friendly mock library. A few blogs mention a unity flavor of NSubstitute, but development
	// doesn't appear to be ongoing, no activity in 3+ years, so let's just do a manual mock instead.
	class MockExplodeWhenShot : ExplodeWhenShot
	{
		public Action<Living> callback;

		internal override void HurtPeople(Living living, string damagedBy, int damage)
		{
			if (callback != null) {
				callback(living);
			}
		}
	}
}
