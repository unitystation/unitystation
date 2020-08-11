using Light2D;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class TimeLimitedDecal : MonoBehaviour
	{
		private SpriteHandler spriteHandler;
		private LightSprite lightSprite;

		private float lifeTime;
		private float currentTime = 0;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			lightSprite = GetComponentInChildren<LightSprite>();
		}

		public void SetUpDecal(float timeToLive)
		{
			lifeTime = timeToLive;
			spriteHandler.PushTexture(false);
			if (lightSprite != null)
			{
				lightSprite.LightOrigin = transform.position;
			}
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