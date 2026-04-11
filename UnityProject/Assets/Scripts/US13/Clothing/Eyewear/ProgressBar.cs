using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using Util;

namespace US13.Clothing.Eyewear
{
	/// <summary>
	/// This class is used to handle the scaling/offset/colouring of sprites in discrete steps to accomodate a progress bar.
	/// This is not a networked class, instead the responsibility of networking is placed on the script using this class.
	/// </summary>
	[ExecuteInEditMode]

	public class ProgressBar : MonoBehaviour
	{
		[Serializable]
		private class AdditionalBarElement
		{
			[SerializeField] private SpriteRenderer _spriteRenderer;
			[SerializeField] private Color _mixColor = Color.white;

			public void UpdateColor(Color newColor)
			{
				_spriteRenderer.color = newColor * _mixColor;
			}

			public void SetVisible(bool isVisible)
			{
				_spriteRenderer.SetActive(isVisible);
			}
		}

		/// <summary>
		/// Value between 0 and 1. Modify this to change the visual of the bar
		/// </summary>
		public float Value
		{
			get => value;
			set
			{
				UpdateValue(value);
				return;
			}
		}

		private bool _isVisible = false;
		private float value = 0;


		[SerializeField, Tooltip("The sprite that will be adjusted. This sprite must be a child with 0 x-coord offset")]
		private SpriteRenderer spriteToModify = null;

		private Transform spriteTransform => spriteToModify?.gameObject.transform;

		[SerializeField] private int steps = 0;
		[SerializeField] private float maxScale = 15;
		[SerializeField] private float minScale = 0;

		[SerializeField] private Gradient colourGradient = new Gradient();

		[SerializeField]
		private List<AdditionalBarElement> additionalElementsToColour = new List<AdditionalBarElement>();

		private float scaleRange => maxScale - minScale;

		private void UpdateValue(float newValue, bool forceUpdate = false)
		{
			if (Application.isPlaying && _isVisible == false) return;
			if (spriteToModify == false) return;

			newValue = Mathf.Clamp(newValue, 0.0f, 1.00f);
			if (forceUpdate == false && newValue.Approx(value)) return;

			float effectiveValue = Mathf.Ceil(newValue * steps) / steps;

			spriteToModify.color = colourGradient.Evaluate(effectiveValue);
			foreach (var additionalBarElement in additionalElementsToColour)
			{
				additionalBarElement.UpdateColor(spriteToModify.color);
			}

			float newScale = minScale + effectiveValue * scaleRange;
			Vector3 currentScale = spriteTransform.localScale;
			spriteTransform.localScale = new Vector3(newScale, currentScale.y, currentScale.z);

			float scalingAmount = spriteTransform.localScale.x - minScale;
			Vector3 newPosition = new Vector3((scalingAmount - minScale) / 2, spriteTransform.localPosition.y, 0);
			spriteTransform.localPosition = newPosition;

			value = newValue;
		}

		public void SetVisible(bool isVisible)
		{
			if (_isVisible == isVisible) return;
			_isVisible = isVisible;

			foreach (var additionalBarElement in additionalElementsToColour)
			{
				additionalBarElement.SetVisible(isVisible);
			}

			spriteToModify.gameObject.SetActive(isVisible);

			UpdateValue(value, true);
		}

		[Button]
		private void TestSetToRandomValue()
		{
			Value = UnityEngine.Random.Range(0.0f, 1.0f);
		}

	}
}