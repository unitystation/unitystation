using System;
using UnityEngine;

namespace UI.Systems
{
	public class MouseFollowUI : MonoBehaviour
	{
		private Vector2 originalPosition = Vector2.zero;
		[SerializeField] private RectTransform self;
		[SerializeField] private float parallaxEffectStrength = 50f;

		private void Start()
		{
			originalPosition = self.anchoredPosition;
		}

		private void Update()
		{
			// Get normalized mouse position (0 to 1 range)
			Vector2 normalizedMousePosition = new(
				Mathf.Clamp01(Input.mousePosition.x / Screen.width),
				Mathf.Clamp01(Input.mousePosition.y / Screen.height));

			// Calculate the offset based on the normalized position
			Vector2 offset = new Vector2(
				(normalizedMousePosition.x - 0.5f) * parallaxEffectStrength,
				(normalizedMousePosition.y - 0.5f) * parallaxEffectStrength);

			// Target position
			Vector2 targetPosition = originalPosition + offset;

			// Smoothly move the UI element
			self.anchoredPosition = Vector2.Lerp(self.anchoredPosition, targetPosition, Time.deltaTime * 5f);
		}
	}
}