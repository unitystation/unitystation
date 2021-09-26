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

		private RadialScroll scroll;

		private RadialDrag drag;

		private AnimatedRadialRotation rotationAnimator;

		protected override float RaycastableArcMeasure => raycastableArcMeasure;

		public RadialScroll Scroll => this.GetComponentByRef(ref scroll);

		public RadialDrag Drag => this.GetComponentByRef(ref drag);

		private PointerEventData PointerEvent { get; } = new PointerEventData(EventSystem.current);

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
				Scroll.ScrollCount = value;
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
			try
			{
				Drag.enabled = value;
				Scroll.enabled = value;
				itemRing.raycastTarget = value;
				itemLabel.raycastTarget = value;
			}
			catch (NullReferenceException exception)
			{
				Logger.LogError("Caught a NRE in ItemRadial.SetRadialScrollable() " + exception.Message, Category.UI);
			}
		}

		public void UpdateArrows()
		{
			var roundedRotation = Mathf.Round(TotalRotation);
			previousArrow.SetActive(roundedRotation > 0);
			nextArrow.SetActive(roundedRotation < Mathf.Round(MaxIndex * ItemArcMeasure));
		}

		public void LateUpdate()
		{
			try
			{
				background.rotation = Quaternion.identity;
				itemLabel.transform.rotation = Quaternion.identity;
			}
			catch (NullReferenceException exception)
			{
				Logger.LogError("Caught a NRE in ItemRadial.LateUpdate() " + exception.Message, Category.UI);
			}
			catch (UnassignedReferenceException exception)
			{
				Logger.LogError("Caught an Unassigned Reference Exception in ItemRadial.LateUpdate() " + exception.Message, Category.UI);
			}
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

		public void CenterItemsTowardsAngle(int count, float angle)
		{
			angle -= Math.Min(count, MaxShownItems) * ItemArcMeasure / 2;
			transform.localEulerAngles = new Vector3(0, 0, angle);
		}

		private void OnCompleteRotation()
		{
			PointerEvent.position = CommonInput.mousePosition;
			OnCompleteRotation(PointerEvent);
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

			LowerMaskItem.ScaleIcon(scale, true);
			UpperMaskItem.ScaleIcon(scale, false);
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
