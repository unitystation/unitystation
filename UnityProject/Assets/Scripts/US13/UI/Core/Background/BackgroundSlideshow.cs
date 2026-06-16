using System.Collections;
using UnityEngine;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Rotates through a set of backgrounds over time, crossfading between two layers for a smooth transition.
	/// </summary>
	public class BackgroundSlideshow : MonoBehaviour
	{
		[SerializeField] private BackgroundSet backgroundSet = null;
		[SerializeField] private BackgroundDisplay frontLayer = null;
		[SerializeField] private BackgroundDisplay backLayer = null;
		[SerializeField] private CanvasGroup frontGroup = null;
		[SerializeField] private CanvasGroup backGroup = null;
		[SerializeField] private float secondsPerImage = 14f;
		[SerializeField] private float crossfadeSeconds = 2.5f;
		[SerializeField] private bool randomOrder = true;

		private int currentIndex = -1;
		private Coroutine slideshowRoutine;

		private void OnEnable()
		{
			if (backgroundSet == null || backgroundSet.Count == 0) return;
			frontGroup.alpha = 1f;
			backGroup.alpha = 0f;
			frontLayer.Show(PickNextBackground());
			slideshowRoutine = StartCoroutine(RunSlideshow());
		}

		private void OnDisable()
		{
			if (slideshowRoutine != null) StopCoroutine(slideshowRoutine);
		}

		private IEnumerator RunSlideshow()
		{
			while (true)
			{
				yield return WaitFor.Seconds(secondsPerImage);
				yield return AdvanceToNextBackground();
			}
		}

		private IEnumerator AdvanceToNextBackground()
		{
			backLayer.Show(PickNextBackground());
			float time = 0f;
			while (time < crossfadeSeconds)
			{
				time += Time.deltaTime;
				float t = crossfadeSeconds > 0f ? Mathf.Clamp01(time / crossfadeSeconds) : 1f;
				frontGroup.alpha = 1f - t;
				backGroup.alpha = t;
				yield return null;
			}
			SwapLayers();
		}

		private void SwapLayers()
		{
			BackgroundDisplay previousFront = frontLayer;
			frontLayer = backLayer;
			backLayer = previousFront;

			CanvasGroup previousFrontGroup = frontGroup;
			frontGroup = backGroup;
			backGroup = previousFrontGroup;

			frontGroup.alpha = 1f;
			backGroup.alpha = 0f;
		}

		private BackgroundImage PickNextBackground()
		{
			if (randomOrder)
			{
				return backgroundSet.GetRandom();
			}
			currentIndex = (currentIndex + 1) % backgroundSet.Count;
			return backgroundSet.GetAt(currentIndex);
		}
	}
}
