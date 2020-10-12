using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI.Core.Events;

namespace UI.Core.Radial
{
	public class Radial<T> : MonoBehaviour, IReadOnlyList<T>, IRadial where T : RadialItem<T>
	{
		[SerializeField]
		private T itemPrefab = default;

		[SerializeField]
		private int maxShownItems = 8;

		[SerializeField]
		private int outerRadius = default;

		[SerializeField]
		private int innerRadius = default;

		[Tooltip("The actual angle to use for the radial. Set lower to fill only a partial circle.")]
		[Range(1, 360)]
		[SerializeField]
		private int fillAngle = 360;

		protected List<T> Items { get; } = new List<T>();

		public PointerEventsListener<T> RadialEvents { get; } = new PointerEventsListener<T>();

	    public T Selected { get; set; }

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

		protected int FillAngle
		{
			get => fillAngle;
			set => fillAngle = Math.Max(1, Math.Min(value, 360));
		}

	    protected int TotalItemCount { get; set; }

	    public Vector3 ItemCenter { get; private set; }

		public int OuterRadius => outerRadius;

	    public int InnerRadius => innerRadius;

	    public float Scale => transform.localScale.x;

	    public float ItemArcAngle => (float)FillAngle / Math.Max(1, ShownItemsCount);

	    protected T ItemPrefab => itemPrefab;

	    protected int MaxShownItems => maxShownItems;

	    public int ShownItemsCount => Math.Min(TotalItemCount, MaxShownItems);

	    public virtual void Setup(int itemCount)
	    {
		    BasicSetup(itemCount);

		    for (var i = 0; i < Math.Max(ShownItemsCount, Count); i++)
		    {
			    InitItem(i);
			    var rotation = new Vector3(0, 0, i * ItemArcAngle);
			    var isActive = i < ShownItemsCount;
			    SetupItem(Items[i], i, rotation, isActive);
		    }
	    }

	    protected void BasicSetup(int itemCount)
	    {
		    transform.localScale = new Vector3(Scale, Scale, 1f);
		    TotalItemCount = itemCount;
		    RotationParent.localEulerAngles = Vector3.zero;
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
		    var angle = Mathf.Deg2Rad * (ItemArcAngle / 2);
		    ItemCenter = new Vector3(-radius * Mathf.Cos(angle), -radius * Mathf.Sin(angle));
	    }

	    public virtual void RotateRadial(float rotation)
	    {
		    rotationParent.Rotate(Vector3.forward, rotation);
	    }

	    public void Invoke(PointerEventType eventType, PointerEventData eventData, T item) =>
		    RadialEvents.Invoke(eventType, eventData, item);

	    protected virtual (float lowerBound, float upperBound) GetItemBounds(int index, float itemRotation) =>
		    (itemRotation, itemRotation + ItemArcAngle);

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
		    return IsPositionValid(position, rotation, rotation + FillAngle, FillAngle, fullRadius);
	    }

	    private bool IsPositionValid(Vector2 position, float lowerBound, float upperBound, float checkAngle, bool fullRadius = false)
	    {
		    var relativePosition = (Vector2)transform.position - position;
		    if (!isActiveAndEnabled || !IsPositionInRadius(relativePosition, fullRadius))
		    {
			    return false;
		    }
		    return Mathf.Round(checkAngle) >= 360 || IsPositionWithinAngle(relativePosition, lowerBound, upperBound);
	    }

	    private bool IsPositionWithinAngle(Vector2 relativePosition, float lowerBound, float upperBound)
	    {
		    lowerBound = Mathf.Round(lowerBound);
		    upperBound = Mathf.Round(upperBound);

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

	    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

	    IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

	    public int Count => Items.Count;

	    public T this[int index] => Items[index];
	}
}
