using System;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class TimeLimitedDecal : MonoBehaviour
	{
		private SpriteHandler spriteHandler;

		private float lifeTime;
		private float currentTime = 0;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		public void SetUpDecal(float timeToLive)
		{
			lifeTime = timeToLive;
			spriteHandler.PushTexture(false);
		}

		private void Update()
		{
			currentTime += Time.deltaTime;
			if (currentTime >= lifeTime)
			{
				currentTime = 0;
				spriteHandler.PushClear(false);
				Despawn.ClientSingle(gameObject);
			}
		}
	}
}