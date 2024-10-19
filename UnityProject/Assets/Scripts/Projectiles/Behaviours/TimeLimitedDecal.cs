using Light2D;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class TimeLimitedDecal : MonoBehaviour
	{
		private SpriteHandler spriteHandler;
		private LightSprite lightSprite;

		[SerializeField] private float lifeTime = 0.5f;
		private float currentTime = 0;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			lightSprite = GetComponentInChildren<LightSprite>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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

		private void UpdateMe()
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