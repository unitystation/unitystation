using TMPro;
using UnityEngine;
using UI.Core.Radial;
using UnityEngine.EventSystems;

namespace UI.Core.RightClick
{
	public class ItemRadial : ScrollableRadial<RightClickRadialButton>
	{
		[SerializeField]
		private ReversibleObjectScale previousArrow = default;

		[SerializeField]
		private ReversibleObjectScale nextArrow = default;

		[SerializeField]
		private RectTransform background = default;

		[SerializeField]
		private TMP_Text itemLabel = default;

		private AnimatedRadialRotation rotationAnimator;

		public AnimatedRadialRotation RotationAnimator
		{
			get
			{
				if (rotationAnimator == null)
				{
					rotationAnimator = GetComponent<AnimatedRadialRotation>();
				}

				rotationAnimator.OnCompleteEvent = OnCompleteRotation;
				return rotationAnimator;
			}
		}

		public override void Setup(int itemCount)
		{
			base.Setup(itemCount);
			itemLabel.SetText(string.Empty);
			previousArrow.transform.SetAsLastSibling();
			nextArrow.transform.SetAsLastSibling();
			UpdateArrows();
		}

		public void UpdateArrows()
		{
			var roundedRotation = Mathf.Round(TotalRotation);
			previousArrow.SetActive(roundedRotation > 0);
			nextArrow.SetActive(roundedRotation < Mathf.Round(MaxIndex * ItemArcMeasure));
		}

		public void LateUpdate()
		{
			background.rotation = Quaternion.identity;
			itemLabel.transform.rotation = Quaternion.identity;
		}

		public override void RotateRadial(float rotation)
		{
			base.RotateRadial(rotation);
			ScaleIconSize();
			TweenArrows(rotation <= 0);
		}

		public void TweenRotation(float rotation)
		{
			if (IsRotationValid(rotation))
			{
				var currentValue = RotationAnimator.CurrentValue;
				var extraRotation = rotation;
				var diff = TotalRotation - currentValue - rotation;
				if (rotation < 0 && diff > MaxIndexAngle)
				{

					extraRotation = -(MaxIndexAngle - TotalRotation + currentValue);
				}
				else if (diff < 0)
				{
					extraRotation = TotalRotation - currentValue;
				}
				Logger.Log($"TotalRotation = {TotalRotation} currentValue = {currentValue} newValue = {extraRotation}");
				RotationAnimator.TweenRotation(extraRotation);
			}
			else
			{
				RotationAnimator.OnCompleteEvent?.Invoke();
			}
		}

		private void OnCompleteRotation()
		{
			var pointerEvent = new PointerEventData(EventSystem.current)
			{
				position = CommonInput.mousePosition
			};
			OnCompleteRotation(pointerEvent);
		}

		private void OnCompleteRotation(PointerEventData pointerEvent)
		{
			foreach (var button in Items)
			{
				button.SetInteractable(true);
			}

			var item = Selected;
			if (item != null && item.IsRaycastLocationValid(pointerEvent.position, null))
			{
				item.OnPointerEnter(pointerEvent);
			}
			else
			{
				Selected = null;
			}
			TweenArrowsBack();
		}

		private void ScaleIconSize()
		{
			var scale = TotalRotation % ItemArcMeasure / ItemArcMeasure;

			if (scale < 0.99f)
			{
				LowerMaskItem.ScaleIcon(LeanTween.easeOutCirc(1, 0, scale));
			}
			else
			{
				LowerMaskItem.ScaleIcon(1);
			}

			UpperMaskItem.ScaleIcon(LeanTween.easeInCirc(0, 1, scale));
		}

		private void TweenArrows(bool forward)
		{
			if (forward)
			{
				nextArrow.TweenScale(AnimationDirection.Forward);
				previousArrow.TweenScale(AnimationDirection.Backward);
			}
			else
			{
				nextArrow.TweenScale(AnimationDirection.Backward);
				previousArrow.TweenScale(AnimationDirection.Forward);
			}
		}

		private void TweenArrowsBack()
		{
			nextArrow.TweenScale(AnimationDirection.Backward);
			previousArrow.TweenScale(AnimationDirection.Backward);
		}

		public void ChangeLabel(string text) => itemLabel.SetText(text);
	}
}
