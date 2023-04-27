using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Core.NetUI
{
	/// Holds value of what sprite to load
	/// sprite-based
	[RequireComponent(typeof(Image))]
	public class NetSpriteImage : NetUIStringElement
	{
		[Tooltip("The sprites this networked image can choose from.")]
		[SerializeField]
		private Sprite[] sprites = default;

		public Image Element => element ??= GetComponent<Image>();
		private Image element;

		private int spriteIndex;

		public override ElementMode InteractionMode => ElementMode.ServerWrite;

		public override string Value {
			get => spriteIndex.ToString() ?? "-1";
			protected set {
				externalChange = true;
				SetSprite(value);
				externalChange = false;
			}
		}

		public void SetSprite(int spriteIndex)
		{
			MasterSetValue(spriteIndex.ToString());
		}

		private void SetSprite(string sprite)
		{
			if (int.TryParse(sprite, out int spriteIndex) == false) return;
			if (spriteIndex > sprites.Length) return;
			if (Element.sprite == sprites[spriteIndex]) return;

			Element.sprite = sprites[spriteIndex];
			this.spriteIndex = spriteIndex;
		}
	}
}
