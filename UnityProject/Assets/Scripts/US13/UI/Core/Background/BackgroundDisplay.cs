using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Core.Background
{
	/// <summary>
	/// Shows a single background image on a UI Image and fits it to the screen as either cover or letterbox.
	/// </summary>
	[RequireComponent(typeof(AspectRatioFitter))]
	public class BackgroundDisplay : MonoBehaviour
	{
		[SerializeField] private Image targetImage = null;
		[SerializeField] private AspectRatioFitter aspectFitter = null;
		[SerializeField] private BackgroundSet backgroundSet = null;
		[SerializeField] private bool randomiseOnEnable = false;
		[SerializeField] private int startIndex = 0;

		private void Awake()
		{
			if (targetImage == null) targetImage = GetComponent<Image>();
			if (aspectFitter == null) aspectFitter = GetComponent<AspectRatioFitter>();
		}

		private void OnEnable()
		{
			if (backgroundSet == null || backgroundSet.Count == 0) return;
			Show(randomiseOnEnable ? backgroundSet.GetRandom() : backgroundSet.GetAt(startIndex));
		}

		public void Show(BackgroundImage background)
		{
			if (background == null || background.Sprite == null) return;
			if (targetImage == null || aspectFitter == null) return;
			targetImage.sprite = background.Sprite;
			FitToScreen(background);
		}

		public void ShowRandom()
		{
			if (backgroundSet == null) return;
			Show(backgroundSet.GetRandom());
		}

		private void FitToScreen(BackgroundImage background)
		{
			Sprite sprite = background.Sprite;
			aspectFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
			aspectFitter.aspectMode = background.Fit == BackgroundFit.Cover
				? AspectRatioFitter.AspectMode.EnvelopeParent
				: AspectRatioFitter.AspectMode.FitInParent;
		}
	}
}
