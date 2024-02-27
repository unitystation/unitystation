using System;
using Objects.Lighting;
using UnityEngine;

namespace Core.Lighting
{
	public class GraphLightAnimator : MonoBehaviour, ILightAnimation
	{
		public bool AnimationActive { get; set; } = false;

		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private LightSource source;
		[SerializeField] private AnimationCurve curve;
		[SerializeField] private float duration = 0.75f;
		[SerializeField] private Color defaultStateOnUpdateEnd = Color.white;
		[SerializeField] private Color colorToAnimateTowards = Color.white;
		private DateTime timeSinceAnimStart = DateTime.Now;

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

		[field: SerializeField] public int ID { get; set; }

		public void AnimateLight()
		{
			float elapsedTime = (float)(DateTime.Now.Ticks - timeSinceAnimStart.Ticks) / TimeSpan.TicksPerSecond / duration;
			float t = curve.Evaluate(elapsedTime);
			Color lerpedColor = Color.Lerp(source.CurrentOnColor, colorToAnimateTowards, t);
			source.lightSprite.Color = lerpedColor;
			if (t >= 1)
			{
				timeSinceAnimStart = DateTime.Now;
				source.lightSprite.Color = defaultStateOnUpdateEnd;
			}
		}

		public void StopAnimation()
		{
			if (AnimationActive == false) return;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			source.lightSprite.Color = defaultStateOnUpdateEnd;
			AnimationActive = false;
		}

		public void StartAnimation()
		{
			if (AnimationActive) return;
			defaultStateOnUpdateEnd = source.CurrentOnColor;
			timeSinceAnimStart = DateTime.Now;
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			AnimationActive = true;
		}

		public void UpdateMe()
		{
			AnimateLight();
		}
	}
}