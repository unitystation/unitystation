using Logs;
using Mirror;
using Objects.Lighting;
using UnityEngine;

namespace Core.Lighting
{
	public class EmergencyLightAnimator : MonoBehaviour, ILightAnimation
	{
		public float rotateSpeed = 40f;

		public Color EmergencyColour = Color.red;

		public Color LightSourceColour = Color.white;

		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private LightSource source;
		[SerializeField] private float speedVariation = 0.25f;
		public Sprite emergancySprite;
		private Sprite previousSprite;
		private bool previouslySet = false;
		private float currentSpeed = 0f;

		SpriteHandler ILightAnimation.SpriteHandler
		{
			get => spriteHandler;
			set => spriteHandler = value;
		}

		LightSource ILightAnimation.Source
		{
			get => source;
			set => source = value;
		}

		[field: SerializeField] public int ID { get; set; } = 0;

		private void OnEnable()
		{
			source ??= GetComponent<LightSource>();
			spriteHandler ??= GetComponentInChildren<SpriteHandler>();
		}

		private void OnDisable()
		{
			StopAnimation();
		}

		public void StartAnimation()
		{
			LightSourceColour = source.CurrentOnColor;
			source.CurrentOnColor = EmergencyColour;
			previousSprite = source.lightSprite.Sprite;
			source.lightSprite.Sprite = emergancySprite;
			currentSpeed = rotateSpeed + Random.Range(0, speedVariation);
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			previouslySet = true;
		}

		public void StopAnimation()
		{
			if (previousSprite)
			{
				source.CurrentOnColor = LightSourceColour;
				source.lightSprite.Sprite = previousSprite;
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			}
			previouslySet = false;
		}

		protected virtual void UpdateMe()
		{
			AnimateLight();
		}

		public void AnimateLight()
		{
			if (source == null || source.mLightRendererObject == null)
			{
				StopAnimation();

				if (this != null && gameObject != null)
				{
					Loggy.LogError($"{gameObject.name} had something null");
				}

				return;
			}
			source.mLightRendererObject.transform.Rotate(0f, 0f, currentSpeed * Time.deltaTime);
		}
	}
}
