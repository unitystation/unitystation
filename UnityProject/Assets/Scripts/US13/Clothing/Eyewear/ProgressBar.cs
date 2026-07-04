using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using Util;
using Color = UnityEngine.Color;

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
			[SerializeField] private bool isCanvasElement = false;
			[HideIf(nameof(isCanvasElement)), SerializeField] private SpriteRenderer _spriteRenderer;
			[ShowIf(nameof(isCanvasElement)), SerializeField] private Image _image;

			[SerializeField] private Color _mixColor = Color.white;

			public void UpdateColor(Color newColor)
			{
				if (isCanvasElement) _image.color = newColor * _mixColor;
				else _spriteRenderer.color = newColor * _mixColor;
			}

			public void SetVisible(bool isVisible)
			{
				if(isCanvasElement) _image.SetActive(isVisible);
				else _spriteRenderer.SetActive(isVisible);
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


		[SerializeField] private bool isCanvasElement = false;
		//must be child with 0 x offset
		[HideIf(nameof(isCanvasElement)), SerializeField] private SpriteRenderer spriteToModify = null;
		[ShowIf(nameof(isCanvasElement)), SerializeField] private Image imageToModify = null;

		private Transform spriteTransform => spriteToModify?.gameObject.transform;
		private RectTransform spriteTransformRect => imageToModify?.rectTransform;

		[SerializeField] private int steps = 0;
		[SerializeField] private float maxScale = 15;
		[SerializeField] private float minScale = 0;

		[SerializeField] private Gradient colourGradient = new Gradient();

		[SerializeField]
		private List<AdditionalBarElement> additionalElementsToColour = new List<AdditionalBarElement>();

		private float scaleRange => maxScale - minScale;

		public void SetGradient(Gradient gradient)
		{
			colourGradient = gradient;
		}

		/// <summary>
		/// Called internally by the setter of Value. only call this is you want to force an update for some other reason
		/// </summary>
		public void UpdateValue(float newValue, bool forceUpdate = false)
		{
			if (Application.isPlaying && _isVisible == false) return;
			if ((isCanvasElement && imageToModify == false) || (isCanvasElement == false && spriteToModify == false)) return;

			newValue = Mathf.Clamp(newValue, 0.0f, 1.00f);
			if (forceUpdate == false && newValue.Approx(value)) return;

			float effectiveValue = Mathf.Ceil(newValue * steps) / steps;

			Color newColour = colourGradient.Evaluate(effectiveValue);

			if (isCanvasElement) imageToModify.color = newColour;
			else spriteToModify.color = newColour;

			foreach (var additionalBarElement in additionalElementsToColour)
			{
				additionalBarElement.UpdateColor(newColour);
			}

			float newScale = minScale + effectiveValue * scaleRange;
			if (isCanvasElement)
			{
				Vector3 currentScale = spriteTransformRect.localScale;
				spriteTransformRect.localScale = new Vector3(newScale, currentScale.y, currentScale.z);

				float scalingAmount = spriteTransformRect.localScale.x - minScale;
				Vector3 newPosition = new Vector3((scalingAmount - minScale) / 2, spriteTransformRect.localPosition.y, 0);
				spriteTransformRect.localPosition = newPosition;
			}
			else
			{
				Vector3 currentScale = spriteTransform.localScale;
				spriteTransform.localScale = new Vector3(newScale, currentScale.y, currentScale.z);

				float scalingAmount = spriteTransform.localScale.x - minScale;
				Vector3 newPosition = new Vector3((scalingAmount - minScale) / 2, spriteTransform.localPosition.y, 0);
				spriteTransform.localPosition = newPosition;
			}

			value = newValue;
		}

		public void SetVisible(bool isVisible)
		{
			_isVisible = isVisible;

			foreach (var additionalBarElement in additionalElementsToColour)
			{
				additionalBarElement.SetVisible(isVisible);
			}
			if(isCanvasElement) imageToModify.gameObject.SetActive(isVisible);
			else spriteToModify.gameObject.SetActive(isVisible);

			if(_isVisible) UpdateValue(value, true);
		}

		[Button]
		private void TestSetToRandomValue()
		{
			Value = UnityEngine.Random.Range(0.0f, 1.0f);
		}
		[Button]
		private void TestSetToOne()
		{
			Value =1.0f;
		}

	}
}