using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.Animations
{
	[RequireComponent(typeof(Image))]
	public class AnimatedImage : MonoBehaviour
	{
		[SerializeField]
		private SpriteDataSO sprites;

		private Image image;
		private int index = 0;
		private float timer = 0;

		private int spriteVarience = 0;

		private List<SpriteDataSO.Frame> frame = new List<SpriteDataSO.Frame>();

		void Awake()
		{
			image = GetComponent<Image>();
		}

		private void Update()
		{
			if((timer+=Time.deltaTime) >= (frame[index].secondDelay / frame.Count))
			{
				timer = 0;
				image.sprite = frame[index].sprite;
				index = (index + 1) % frame.Count;
			}
		}

		public void SetVariant(int newIndex)
		{
			frame = sprites.Variance[newIndex].Frames;
		}

		public void SetSprite(SpriteDataSO newSO)
		{
			sprites = newSO;
			SetVariant(0);
		}
	}
}