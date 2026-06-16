using System.Collections;
using UnityEngine;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Frames the screen with letterbox bars top and bottom, optionally sliding them in when shown.
	/// </summary>
	public class CinematicBars : MonoBehaviour
	{
		[SerializeField] private RectTransform topBar = null;
		[SerializeField] private RectTransform bottomBar = null;
		[SerializeField, Range(0f, 0.3f)] private float barHeight = 0.12f;
		[SerializeField] private bool slideInOnEnable = true;
		[SerializeField] private float slideDuration = 1f;

		private Coroutine slideRoutine;

		private void OnEnable()
		{
			if (slideInOnEnable)
			{
				slideRoutine = StartCoroutine(SlideBarsIn());
				return;
			}
			SetBarHeight(barHeight);
		}

		private void OnDisable()
		{
			if (slideRoutine != null) StopCoroutine(slideRoutine);
		}

		public void SetBarHeight(float height)
		{
			ApplyHeight(topBar, height);
			ApplyHeight(bottomBar, height);
		}

		private IEnumerator SlideBarsIn()
		{
			float time = 0f;
			SetBarHeight(0f);
			while (time < slideDuration)
			{
				time += Time.deltaTime;
				float t = slideDuration > 0f ? Mathf.Clamp01(time / slideDuration) : 1f;
				SetBarHeight(Mathf.Lerp(0f, barHeight, Mathf.SmoothStep(0f, 1f, t)));
				yield return null;
			}
			SetBarHeight(barHeight);
		}

		private void ApplyHeight(RectTransform bar, float height)
		{
			if (bar == null) return;
			float reference = bar.parent is RectTransform parent ? parent.rect.height : Screen.height;
			Vector2 size = bar.sizeDelta;
			size.y = reference * height;
			bar.sizeDelta = size;
		}
	}
}
