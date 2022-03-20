using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UI.Core.Radial;
using UI.Core.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
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
		private Graphic itemRing;

		[SerializeField]
		private TMP_Text itemLabel = default;

		private float raycastableArcMeasure;

		private RadialScroll scroll;

		private RadialDrag drag;

		private AnimatedRadialRotation rotationAnimator;

		private ReversibleObjectScale PreviousArrow =>
			VerifyChildReference(ref previousArrow, $"{nameof(PreviousArrow)} to an image with ReversibleObjectScale object", "PreviousArrow");

		private ReversibleObjectScale NextArrow =>
			VerifyChildReference(ref nextArrow, $"{nameof(NextArrow)} to an image with ReversibleObjectScale object", "NextArrow");

		private Graphic ItemRing =>
			VerifyChildReference(ref itemRing, $"{nameof(ItemRing)} to a SVG graphic object", "RadialItemRing");

		private TMP_Text ItemLabel =>
			VerifyChildReference(ref itemLabel, $"{nameof(ItemLabel)} to a TMP text object", "ItemLabel");

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

			ItemLabel.OrNull()?.SetText(string.Empty);
			PreviousArrow.OrNull()?.transform.SetAsLastSibling();
			NextArrow.OrNull()?.transform.SetAsLastSibling();
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
			Scroll.enabled = value;

			if (ItemRing == null || ItemLabel == null) return;

			ItemRing.raycastTarget = value;
			ItemLabel.raycastTarget = value;
		}

		public void UpdateArrows()
		{
			var roundedRotation = Mathf.Round(TotalRotation);
			PreviousArrow.OrNull()?.SetActive(roundedRotation > 0);
			NextArrow.OrNull()?.SetActive(roundedRotation < Mathf.Round(MaxIndex * ItemArcMeasure));
		}

		public void LateUpdate()
		{
			if (ItemRing == null || ItemLabel == null) return;

			ItemRing.transform.rotation = Quaternion.identity;
			ItemLabel.transform.rotation = Quaternion.identity;
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
			NextArrow.OrNull()?.TweenScale(forward);
			PreviousArrow.OrNull()?.TweenScale(!forward);
		}

		private void TweenArrowsBack()
		{
			NextArrow.OrNull()?.TweenScale(AnimationDirection.Backward);
			PreviousArrow.OrNull()?.TweenScale(AnimationDirection.Backward);
		}

		public void ChangeLabel(string text) => ItemLabel.OrNull()?.SetText(text);
	}
}
