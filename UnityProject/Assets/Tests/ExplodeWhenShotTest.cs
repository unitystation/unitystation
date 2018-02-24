using System;
using System.Collections;
using NUnit.Framework;
using Sprites;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public class ExplodeWhenShotTest
{
	private SpriteRenderer spriteRenderer;
	private MockExplodeWhenShot subject;

	[SetUp]
	public void SetUp()
	{
		GameObject obj = new GameObject();
		obj.AddComponent<SoundManager>();
		obj.AddComponent<ItemFactory>();
		obj.AddComponent<PoolManager>();
		obj.AddComponent<SpriteManager>();
		spriteRenderer = obj.AddComponent<SpriteRenderer>();
		subject = obj.AddComponent<MockExplodeWhenShot>();
		subject.spriteRend = spriteRenderer;
	}

	[UnityTest]
	public IEnumerator Should_Destroy_Bullet()
	{
		GameObject bullet = new GameObject();
		PoolManager.Instance.PoolClientInstantiate(bullet, Vector2.zero, Quaternion.identity);
		BoxCollider2D collider = bullet.AddComponent<BoxCollider2D>();
		PoolPrefabTracker tracker = bullet.AddComponent<PoolPrefabTracker>();
		tracker.myPrefab = bullet;
		bullet.AddComponent<Bullet_12mm>();

		try
		{
			subject.SendMessage("OnTriggerEnter2D", collider);

			yield return 0;

			Assert.That(!bullet.activeSelf);
			Assert.That(subject.wentBoom);
		}
		finally
		{
			Object.Destroy(bullet);
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
		GameObject player = new GameObject();

		player.AddComponent<BoxCollider2D>();
		HealthBehaviour living = player.AddComponent<HealthBehaviour>();
		player.layer = LayerMask.NameToLayer("Players");

		HealthBehaviour damaged = null;
		subject.callback = t => damaged = t;

		try
		{
			subject.Explode(null);

			Assert.That(living == damaged);
		}
		finally
		{
			Object.Destroy(player);
		}
	}

	[Test]
	public void Should_Not_Damage_Player_Through_Wall()
	{
		GameObject player = new GameObject();
		player.AddComponent<BoxCollider2D>();
		player.AddComponent<HealthBehaviour>();
		player.layer = LayerMask.NameToLayer("Players");
		player.transform.position = new Vector3(2, 0);

		GameObject wall = new GameObject();
		wall.AddComponent<BoxCollider2D>();
		wall.layer = LayerMask.NameToLayer("Walls");
		wall.transform.position = new Vector3(1, 0);

		HealthBehaviour damaged = null;
		subject.callback = t => damaged = t;

		subject.Explode(null);

		Assert.That(damaged == null);
	}

	// TODO: Add a unity-friendly mock library. A few blogs mention a unity flavor of NSubstitute, but development
	// doesn't appear to be ongoing, no activity in 3+ years, so let's just do a manual mock instead.
	private class MockExplodeWhenShot : ExplodeWhenShot
	{
		public Action<HealthBehaviour> callback;
		public bool wentBoom;

		//		internal override void HurtPeople(Living living, string damagedBy, int damage)
		//		{
		//			if (callback != null) {
		//				callback(living);
		//			}
		//		}

		internal override void GoBoom()
		{
			wentBoom = true;
		}
	}
}