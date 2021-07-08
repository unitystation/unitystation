using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using NaughtyAttributes;

namespace Effects
{
	public class AnimateIcon : MonoBehaviour
	{
		[SerializeField, BoxGroup("References")]
		private Image image = default;

		[SerializeField, BoxGroup("Settings")]
		private Vector3 expandedScale = new Vector3(1.5f, 1.5f, 1);
		[SerializeField, BoxGroup("Settings"), Range(0, 5)]
		private float expansionTime = 1f;
		[SerializeField, BoxGroup("Settings"), Range(0, 5)]
		private float contractionTime = 1f;
		[SerializeField, BoxGroup("Settings"), Range(0, 5)]
		private float suspenseTime = 1f;

		[InfoBox("See https://easings.net for easing examples.", EInfoBoxType.Normal), HorizontalLine]

		[SerializeField, BoxGroup("Settings")]
		private bool useCustomExpansionCurve = false;
		[SerializeField, BoxGroup("Settings"), HideIf(nameof(useCustomExpansionCurve))]
		private LeanTweenType expansionEaseType = default;
		[SerializeField, BoxGroup("Settings"), ShowIf(nameof(useCustomExpansionCurve))]
		private AnimationCurve customExpansionCurve = default;

		[SerializeField, BoxGroup("Settings")]
		private bool useCustomContractionCurve = false;
		[SerializeField, BoxGroup("Settings"), HideIf(nameof(useCustomContractionCurve))]
		private LeanTweenType contractionEaseType = default;
		[SerializeField, BoxGroup("Settings"), ShowIf(nameof(useCustomContractionCurve))]
		private AnimationCurve customContractionCurve = default;

		private Vector3 previousSize;

		private void OnValidate()
		{
			if (expandedScale.z != 1f)
			{
				// Z must remain 1, else it can be scaled behind other UI elements and "disappear".
				Vector3 newSize = expandedScale;
				newSize.z = 1;
				expandedScale = newSize;
			}
		}

		public void TriggerAnimation()
		{
			previousSize = image.transform.localScale;

			Expand();
		}

		private void Expand()
		{
			if (useCustomExpansionCurve)
			{
				LeanTween.scale(image.gameObject, expandedScale, expansionTime).setEase(customExpansionCurve).setOnComplete(Contract);
			}
			else
			{
				LeanTween.scale(image.gameObject, expandedScale, expansionTime).setEase(expansionEaseType).setOnComplete(Contract);
			}
		}

		private void Contract()
		{
			if (useCustomContractionCurve)
			{
				LeanTween.scale(image.gameObject, previousSize, contractionTime).setDelay(suspenseTime).setEase(customContractionCurve);
			}
			else
			{
				LeanTween.scale(image.gameObject, previousSize, contractionTime).setDelay(suspenseTime).setEase(contractionEaseType);
			}
		}
	}
}
