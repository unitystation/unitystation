using System.Collections.Generic;
using Objects.Lighting;
using UnityEngine;

namespace Core.Lighting.Animations
{
	public class MultiFrameSpriteAnimation : MonoBehaviour, ILightAnimation
	{
		[SerializeField] private SpriteHandler spriteHandler;
		[SerializeField] private LightSource source;
		[SerializeField] private List<Sprite> sprites = new List<Sprite>();
		[SerializeField] private bool resetToDefaultSpriteOnAnimStop = false;
		[SerializeField] private float animSpeed = 0.2f;
		[field: SerializeField] public int ID { get; set; } = 2;

		private int currentIndex = 0;
		private Sprite defaultSprite = null;

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

		public void AnimateLight()
		{
			source.lightSprite.Sprite = sprites[currentIndex];
			currentIndex++;
			if (currentIndex >= sprites.Count - 1) currentIndex = 0;
		}

		public void StopAnimation()
		{
			source.lightSprite.Sprite = sprites[0];
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, AnimateLight);
			if (resetToDefaultSpriteOnAnimStop)
			{
				source.lightSprite.Sprite = defaultSprite;
			}
		}

		public void StartAnimation()
		{
			defaultSprite = source.lightSprite.Sprite;
			UpdateManager.Add(AnimateLight, animSpeed);
		}
	}
}