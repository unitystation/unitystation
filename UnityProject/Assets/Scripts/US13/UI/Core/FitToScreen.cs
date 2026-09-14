using System;
using NaughtyAttributes;
using UnityEngine;

namespace US13.UI.Core
{
	/// <summary>
	/// Makes a UI window automatically scale itself to the screen,
	/// so it doesn't remain mega-small due to unity having shit UI tools to check the scale of things.
	/// Only gets set on awake to avoid overriding user preference for the scroll-zoom feature.
	/// </summary>
	public class FitToScreen : MonoBehaviour
	{
		private RectTransform rectTransform;
		[SerializeField] private float padding = 20f;
		[SerializeField] private float multiplicativeSize = 4f;
		[SerializeField] private float minimumScale = 2f;

		private void Awake()
		{
			rectTransform = GetComponent<RectTransform>();
			Fit();
		}

		[Button]
		private void Fit()
		{
			if (rectTransform == null) return;
			Vector2 windowSize = rectTransform.rect.size;
			float availableWidth = (Screen.width - padding) * multiplicativeSize;
			float availableHeight = (Screen.height - padding) * multiplicativeSize;
			float scaleX = availableWidth / windowSize.x;
			float scaleY = availableHeight / windowSize.y;
			float scale = Mathf.Min(scaleX, scaleY);
			scale = Mathf.Min(scale, minimumScale);
			rectTransform.localScale = new Vector3(scale, scale, 1f);
		}
	}
}