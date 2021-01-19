using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UI.Core.Radial;
using UI.Core.Animations;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
		private Graphic itemRing = default;

		[SerializeField]
		private TMP_Text itemLabel = default;

		private float raycastableArcMeasure;

		private RadialMouseWheelScroll mouseWheelScroll;

		private RadialDrag drag;

		private AnimatedRadialRotation rotationAnimator;

		protected override float RaycastableArcMeasure => raycastableArcMeasure;

		public RadialMouseWheelScroll MouseWheelScroll => this.GetComponentByRef(ref mouseWheelScroll);

		public RadialDrag Drag => this.GetComponentByRef(ref drag);

		private AnimatedRadialRotation RotationAnimator
		{
			get
			{
				if (rotationAnimator != null)
				{
					return rotationAnimator;
				}
				rotationAnimator = GetComponent<AnimatedRadialRotation>();
				rotationAnimator.OnCompleteEvent = OnCompleteRotation;
				return rotationAnimator;
			}
		}

		public override int MaxShownItems
		{
			get => maxShownItems;
			set
			{
				maxShownItems = value;
				MouseWheelScroll.ScrollCount = value;
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

		public void SetupWithItems(IList<RightClickMenuItem> items)
		{
			Setup(Math.Max(items.Count, MaxShownItems));
			raycastableArcMeasure = items.Count < MaxShownItems ? items.Count * ItemArcMeasure : 360;
			SetRadialScrollable(items.Count > MaxShownItems);

			foreach (var item in Items)
			{
				var index = item.Index;
				if (index < items.Count)
				{
					item.ChangeItem(items[index]);
				}
				else
				{
					item.DisableItem();
				}
			}
		}

		private void SetRadialScrollable(bool value)
		{
			Drag.enabled = value;
			MouseWheelScroll.enabled = value;
			itemRing.raycastTarget = value;
			itemLabel.raycastTarget = value;
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

		public bool TryBeginRotation(float rotation)
		{
			if (IsRotationValid(rotation) == false)
			{
				return false;
			}

			SetItemsInteractable(false);
			Selected.OrNull()?.ResetState();
			TweenRotation(rotation);
			return true;
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
				RotationAnimator.TweenRotation(extraRotation);
			}
			else
			{
				RotationAnimator.OnCompleteEvent?.Invoke();
			}
		}

		public void SetItemsInteractable(bool active)
		{
			if (!active)
			{
				ChangeLabel(string.Empty);
			}

			foreach (var button in Items)
			{
				button.SetInteractable(active);
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
			SetItemsInteractable(true);

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

			if (Mathf.Round(scale * 10) / 10 < 1)
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
			nextArrow.TweenScale(forward);
			previousArrow.TweenScale(!forward);
		}

		private void TweenArrowsBack()
		{
			nextArrow.TweenScale(AnimationDirection.Backward);
			previousArrow.TweenScale(AnimationDirection.Backward);
		}

		public void ChangeLabel(string text) => itemLabel.SetText(text);
	}
}
