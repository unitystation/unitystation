using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Logs;
using UnityEngine;
using UnityEngine.EventSystems;
using UI.Core.Events;
using Util;

namespace UI.Core.Radial
{
	public class Radial<T> : MonoBehaviour, IReadOnlyList<T>, IRadial where T : RadialItem<T>
	{
		[SerializeField]
		private T itemPrefab = default;

		[SerializeField]
		protected int maxShownItems = 8;

		[SerializeField]
		private int outerRadius = default;

		[SerializeField]
		private int innerRadius = default;

		[Tooltip("The arc measure to use for the radial. Set lower to fill only a partial circle.")]
		[Range(1, 360)]
		[SerializeField]
		private int arcMeasure = 360;

		private Transform rotationParent;

		public Transform RotationParent {
			get
			{
				if (rotationParent != null)
				{
					return rotationParent;
				}

				var go = new GameObject("RotationParent");
				var goTransform = go.transform;
				goTransform.SetParent(transform);
				goTransform.localPosition = Vector3.zero;
				goTransform.localScale = Vector3.one;
				rotationParent = goTransform;
				return rotationParent;
			}
		}

		protected int ArcMeasure
		{
			get => arcMeasure;
			set => arcMeasure = Math.Max(1, Math.Min(value, 360));
		}

		public virtual int MaxShownItems
		{
			get => maxShownItems;
			set => maxShownItems = value;
		}

		public T Selected { get; set; }

		protected int TotalItemCount { get; private set; }

		public float ItemArcMeasure { get; private set; }

		public Vector3 ItemCenter { get; private set; }

		protected List<T> Items { get; } = new List<T>();

		public PointerEventsListener<T> RadialEvents { get; } = new PointerEventsListener<T>();

		public bool IsActive => gameObject.activeSelf;

		public int OuterRadius => outerRadius;

		public int InnerRadius => innerRadius;

		public float Scale => transform.localScale.x;

		protected T ItemPrefab => itemPrefab;

		protected virtual float RaycastableArcMeasure => ArcMeasure;

		public int ShownItemsCount => Math.Min(TotalItemCount, MaxShownItems);

		/// Convenience method for verifying radial references.
		/// <see cref="ComponentExtensions.VerifyChildReference"/>
		protected V VerifyChildReference<V>(ref V component, string refDescription,
			string childName = null, [CallerMemberName] string refName = "")
			where V : UnityEngine.Object =>
			this.VerifyChildReference(ref component, refDescription, childName, refName, Category.UI);

		public virtual void Setup(int itemCount)
		{
			BasicSetup(itemCount);

			for (var i = 0; i < Math.Max(ShownItemsCount, Count); i++)
			{
				InitItem(i);
				var rotation = new Vector3(0, 0, i * ItemArcMeasure);
				var isActive = i < ShownItemsCount;
				SetupItem(Items[i], i, rotation, isActive);
			}
		}

		protected void BasicSetup(int itemCount)
		{
			transform.localScale = new Vector3(Scale, Scale, 1f);
			RotationParent.localEulerAngles = Vector3.zero;
			TotalItemCount = itemCount;
			ItemArcMeasure = (float)arcMeasure / Math.Max(1, ShownItemsCount);
			CalcItemCenter();
		}

		protected void InitItem(int index)
		{
			if (index >= Count)
			{
				Items.Add(Instantiate(ItemPrefab, RotationParent.transform));
			}
		}

		protected void SetupItem(T item, int index, Vector3 rotation, bool isActive)
		{
			item.Setup(this, index);
			item.transform.localEulerAngles = rotation;
			item.SetActive(isActive);
		}

		private void CalcItemCenter()
		{
			var radius = OuterRadius - (OuterRadius - InnerRadius) / 2f;
			var angle = Mathf.Deg2Rad * (ItemArcMeasure / 2);
			ItemCenter = new Vector3(-radius * Mathf.Cos(angle), -radius * Mathf.Sin(angle));
		}

		public virtual void RotateRadial(float rotation)
		{
			rotationParent.Rotate(Vector3.forward, rotation);
		}

		public void Invoke(PointerEventType eventType, PointerEventData eventData, T item) =>
			RadialEvents.Invoke(eventType, eventData, item);

		protected virtual (float lowerBound, float upperBound) GetItemBounds(int index, float itemRotation) =>
			(itemRotation, itemRotation + ItemArcMeasure);

		public bool IsItemRaycastable(RadialItem<T> item, Vector2 screenPosition)
		{
			var itemRotation = item.transform.eulerAngles.z;
			var (lowerBound, upperBound) = GetItemBounds(item.Index, itemRotation);
			return IsPositionValid(screenPosition, lowerBound, upperBound, itemRotation);
		}

		public bool IsPositionWithinRadial(Vector2 position) =>
			IsPositionWithinRadial(position,false);

		public bool IsPositionWithinRadial(Vector2 position, bool fullRadius)
		{
			var rotation = transform.eulerAngles.z;
			return IsPositionValid(position, rotation, rotation + RaycastableArcMeasure, RaycastableArcMeasure, fullRadius);
		}

		private bool IsPositionValid(Vector2 position, float lowerBound, float upperBound, float checkAngle, bool fullRadius = false)
		{
			var relativePosition = (Vector2)transform.position - position;
			return isActiveAndEnabled
				&& IsPositionInRadius(relativePosition, fullRadius)
				&& (checkAngle >= 360 || IsPositionWithinAngle(relativePosition, lowerBound, upperBound));
		}

		private bool IsPositionWithinAngle(Vector2 relativePosition, float lowerBound, float upperBound)
		{
			lowerBound = Mathf.Round(lowerBound * 10) / 10;
			upperBound = Mathf.Round(upperBound * 10) / 10;

			// Using Equals as Codacy rightfully doesn't like float equality. In this case it is fine as both values are rounded and it serves its purpose.
			if (lowerBound.Equals(upperBound))
			{
				return false;
			}

			if (upperBound > 360)
			{
				upperBound -= 360;
			}

			var angle = (Mathf.Rad2Deg * Mathf.Atan2(relativePosition.y, relativePosition.x) + 360) % 360;

			if (lowerBound >= upperBound)
			{
				return angle >= lowerBound || angle <= upperBound;
			}
			return angle >= lowerBound && angle <= upperBound;
		}

		private bool IsPositionInRadius(Vector2 relativePosition, bool fullRadius = false)
		{
			var lossyScale = transform.lossyScale.x;
			var inner = fullRadius ? 0 : InnerRadius * lossyScale;
			return relativePosition.IsInRadius(OuterRadius * lossyScale, inner);
		}

		public void OnDisable()
		{
			Selected = null;
		}

		public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

		public int Count => Items.Count;

		public T this[int index] => Items[index];
	}
}
