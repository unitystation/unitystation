using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Objects.Wallmounts
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SpriteRenderer))] // hack to have OnWillRenderObject() called
	public class WallmountTextBehavior : WallmountSpriteBehavior
	{
		private Text text;

		private void Awake()
		{
			text = GetComponent<Text>();
			base.Awake();
		}

		protected override void SetAlpha(int alpha)
		{
			text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
		}
	}
}
