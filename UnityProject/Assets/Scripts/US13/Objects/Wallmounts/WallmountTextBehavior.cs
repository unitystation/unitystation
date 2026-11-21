using UnityEngine;
using UnityEngine.UI;

namespace US13.Objects.Wallmounts
{
	[DisallowMultipleComponent]
	[RequireComponent(typeof(SpriteRenderer))] // hack to have OnWillRenderObject() called
	public class WallmountTextBehavior : WallmountSpriteBehavior
	{
		private Text text;

		protected override void Awake()
		{
			text = GetComponent<Text>();
			base.Awake();
		}

		protected override void SetAlpha(float alpha)
		{
			if (text == null) return;
			var color = text.color;
			if (color.a == alpha) return;
			color.a = alpha;
			text.color = color;
		}
	}
}
