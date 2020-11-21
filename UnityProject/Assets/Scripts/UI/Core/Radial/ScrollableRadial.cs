using System;
using Light2D;
using UnityEngine;

namespace UI.Core.Radial
{
	public class ScrollableRadial<T> : Radial<T> where T : RadialItem<T>
	{
		[SerializeField]
		protected RectTransform lowerMask;

		[SerializeField]
		protected RectTransform upperMask;

		private T lowerMaskItem;

		private T upperMaskItem;

		private float totalRotation;

		protected float TotalRotation
		{
			get => totalRotation;
			private set => totalRotation = Mathf.Max(0, Mathf.Min(value, MaxIndexAngle));
		}

		protected T LowerMaskItem { get; private set; }

		protected T UpperMaskItem { get; private set; }

		private int CurrentIndex { get; set; }

		public Action<T> OnIndexChanged { get; set; }

		public float NearestItemAngle
		{
			get
			{
				var angle = TotalRotation % ItemArcMeasure;
				return angle >= ItemArcMeasure / 2 ? angle - ItemArcMeasure : angle;
			}
		}

		protected int MaxIndex => TotalItemCount - ShownItemsCount;

		protected float MaxIndexAngle => MaxIndex * ItemArcMeasure;

		private T InitMaskItem(RectTransform parent, int index)
		{
			InitItem(index);
			var maskItem = Items[index];
			maskItem.transform.SetParent(parent);

			return maskItem;
		}

		public void Awake()
		{
			LowerMaskItem = InitMaskItem(lowerMask, 0);
			UpperMaskItem = InitMaskItem(upperMask, 1);
		}

		public override void Setup(int itemCount)
		{
			BasicSetup(itemCount);
			TotalRotation = 0;
			CurrentIndex = 0;

			var isScrollable = itemCount > ShownItemsCount;
			upperMask.localEulerAngles = new Vector3(0, 0, ArcMeasure - 360);
			SetupItem(LowerMaskItem, 0, Vector3.zero, isScrollable);
			SetupItem(UpperMaskItem, ShownItemsCount, Vector3.zero, isScrollable);
			for (var i = 2; i < Math.Max(2 + ShownItemsCount, Count); i++)
			{
				InitItem(i);
				var index = isScrollable ? i - 1 : i - 2;
				var rotation = new Vector3(0, 0, index * ItemArcMeasure);
				var isActive = index < ShownItemsCount;
				SetupItem(Items[i], index, rotation, isActive);
			}
		}

		public bool IsRotationValid(float rotation) =>
			TotalRotation > 0.01f && rotation > 0 || TotalRotation < MaxIndexAngle - 0.01f && rotation < 0;

		private void ChangeIndex(int newIndex)
		{
			if (newIndex == CurrentIndex)
			{
				return;
			}

			var delta = newIndex - CurrentIndex;

			ChangeUnmaskedIndices(Mathf.Abs(delta), delta > 0 ? CurrentIndex : newIndex, newIndex);
			LowerMaskItem.Index = newIndex;
			UpperMaskItem.Index = (newIndex + ShownItemsCount) % TotalItemCount;
			OnIndexChanged?.Invoke(LowerMaskItem);
			OnIndexChanged?.Invoke(UpperMaskItem);
			CurrentIndex = newIndex;
		}

		private void ChangeUnmaskedIndices(int delta, int startIndex, int newIndex)
		{
			var itemCount = ShownItemsCount - 1;
			var changeCount = Math.Min(delta, itemCount);
			var first = newIndex % itemCount;

			// The idea here is to counter-rotate only the changed unmasked items so that we only need to update
			// those items and not the whole radial.
			for (var i = 0; i < changeCount; i++)
			{
				var radialIndex = (startIndex + i) % itemCount;
				var indexOffset = 0;
				if (radialIndex > first)
				{
					indexOffset = radialIndex - first;
				}
				else if (radialIndex < first)
				{
					indexOffset = itemCount - first + radialIndex;
				}
				radialIndex += 2; // Offset by the masks
				var item = Items[radialIndex];

				item.Index = newIndex + 1 + indexOffset;

				var itemAngle = (item.Index - newIndex) * ItemArcMeasure;
				item.transform.eulerAngles = new Vector3(0, 0, LowerMaskItem.transform.eulerAngles.z + itemAngle);
				OnIndexChanged?.Invoke(item);
			}
		}

		public override void RotateRadial(float rotation)
		{
			if (IsRotationValid(rotation) == false)
			{
				return;
			}

			TotalRotation -= rotation;

			if (TotalRotation >= MaxIndexAngle)
			{
				SetLocalAngles(-TotalRotation, 0);
			}
			else if (TotalRotation <= 0)
			{
				SetLocalAngles(0, 0);
			}
			else
			{
				SetLocalAngles(-TotalRotation, -(TotalRotation % ItemArcMeasure));
			}

			ChangeIndex(Math.Min(MaxIndex, (int)(TotalRotation / ItemArcMeasure)));
		}

		private void SetLocalAngles(float parentAngle, float maskAngle)
		{
			RotationParent.localEulerAngles = Vector3.zero.WithZ(parentAngle);
			SetMasksLocalAngle(Vector3.zero.WithZ(maskAngle));
		}

		private void SetMasksLocalAngle(Vector3 angle)
		{
			LowerMaskItem.transform.localEulerAngles = angle;
			UpperMaskItem.transform.localEulerAngles = angle;
		}

		protected override (float lowerBound, float upperBound) GetItemBounds(int index, float itemRotation)
		{
			var lowerBound = itemRotation;
			var upperBound = itemRotation + ItemArcMeasure;
			var localAngle = transform.localEulerAngles.z;

			if (index == LowerMaskItem.Index)
			{
				lowerBound = localAngle;
			}

			if (index == UpperMaskItem.Index)
			{
				upperBound = localAngle;
			}

			return (lowerBound, upperBound);
		}
	}
}
