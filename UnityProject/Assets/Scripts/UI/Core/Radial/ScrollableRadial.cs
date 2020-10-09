using System;
using UnityEngine;
using UnityEngine.Events;

namespace UI.Core.Radial
{
	public class ScrollableRadial<T> : Radial<T> where T : RadialItem<T>
	{
		public class IndexChangedEvent : UnityEvent<T> {}

		[SerializeField]
		protected RectTransform lowerMask;

		[SerializeField]
		protected RectTransform upperMask;

		[Tooltip("The number of items to scroll when using the mouse wheel.")]
		[SerializeField]
		protected int mouseWheelScroll;

	    private T lowerMaskItem;
	    private T upperMaskItem;

	    private T InitOrGetMaskItem(ref T maskItem, RectTransform parent, int index)
	    {
		    if (Items.Count <= index)
		    {
			    Items.Add(Instantiate(itemData.radialItemPrefab, RotationParent.transform));
		    }
		    if (maskItem == null)
		    {
			    maskItem = Items[index];
			    maskItem.transform.SetParent(parent);
		    }

		    return maskItem;
	    }

	    protected T LowerMaskItem => InitOrGetMaskItem(ref lowerMaskItem, lowerMask, 0);

	    protected T UpperMaskItem => InitOrGetMaskItem(ref upperMaskItem, upperMask, 1);

	    protected readonly IndexChangedEvent onIndexChanged = new IndexChangedEvent();

	    public IndexChangedEvent OnIndexChanged => onIndexChanged;

	    protected int MaxIndex => TotalItemCount - ShownItemsCount;

	    protected float MaxIndexAngle => MaxIndex * ItemArcAngle;

	    public int CurrentIndex { get; private set; }

	    private float totalRotation;

	    protected float TotalRotation
	    {
		    get => totalRotation;
		    private set => totalRotation = Mathf.Max(0, Mathf.Min(value, MaxIndexAngle));
	    }

	    public float NearestItemAngle
	    {
		    get
		    {
			    var angle = TotalRotation % ItemArcAngle;
			    return angle >= ItemArcAngle / 2 ? angle - ItemArcAngle : angle;
		    }
	    }

	    public override void Setup(int itemCount)
	    {
		    transform.localScale = new Vector3(Scale, Scale, 1f);
		    RotationParent.localEulerAngles = Vector3.zero;
		    TotalItemCount = itemCount;
		    TotalRotation = 0;
		    CurrentIndex = 0;

		    var isScrollable = itemCount > ShownItemsCount;
		    upperMask.localEulerAngles = new Vector3(0, 0, FillAngle - 360);
		    SetupItem(LowerMaskItem, 0, Vector3.zero, isScrollable);
		    SetupItem(UpperMaskItem, ShownItemsCount, new Vector3(0, 0 , ShownItemsCount * ItemArcAngle), isScrollable);
		    for (var i = 2; i < Math.Max(2 + ShownItemsCount, Count); i++)
		    {
			    if (i >= Count)
			    {
				    Items.Add(Instantiate(itemData.radialItemPrefab, RotationParent.transform));
			    }

			    var index = isScrollable ? i - 1 : i - 2;
			    var rotation = new Vector3(0, 0, index * ItemArcAngle);
			    var isActive = index < ShownItemsCount;
			    SetupItem(Items[i], index, rotation, isActive);
		    }
	    }

	    public void Update()
	    {
		    if (!IsPositionWithinRadial(CommonInput.mousePosition, true))
		    {
			    return;
		    }

		    var scrollDelta = Input.mouseScrollDelta.y;
		    // Allow mouse wheel to scroll through items.
		    if (scrollDelta < 0 && TotalRotation > 0)
		    {
			    RotateRadial(ItemArcAngle * mouseWheelScroll);
		    }
		    else if (scrollDelta > 0 && TotalRotation < MaxIndexAngle)
		    {
			    RotateRadial(-ItemArcAngle * mouseWheelScroll);
		    }
	    }

	    private void ChangeIndex(int newIndex)
	    {
		    if (newIndex == CurrentIndex)
		    {
			    return;
		    }

		    var shownItems = ShownItemsCount;
		    var delta = newIndex - CurrentIndex;

		    void ChangeUnmaskedIndices(float angle, int startIndex, int indexModifier)
		    {
			    var unfilled = Mathf.Sign(delta) * (FillAngle - 360);
			    var posDelta = Math.Abs(delta);
			    var rotation = angle * (1 + posDelta / shownItems) + unfilled;
			    // Rotate the changed unmasked items around the masks and set their new index
			    for (var i = 0; i < Math.Min(posDelta, shownItems - 1); i++)
			    {
				    var itemIndex = (startIndex + i) % (shownItems - 1) + 2;
				    Items[itemIndex].transform.Rotate(Vector3.back, rotation);
				    Items[itemIndex].Index = startIndex + i + indexModifier;
				    onIndexChanged.Invoke(Items[itemIndex]);
			    }
		    }

		    float rotate;

		    if (delta > 0)
		    {
			    rotate = ItemArcAngle;
			    ChangeUnmaskedIndices(rotate, CurrentIndex, shownItems + delta / shownItems);
		    }
		    else
		    {
			    rotate = -ItemArcAngle;
			    ChangeUnmaskedIndices(rotate, newIndex, 1);
		    }

		    LowerMaskItem.Index = newIndex;
		    UpperMaskItem.Index = (newIndex + shownItems) % TotalItemCount;
		    RotateMasks(rotate);
		    onIndexChanged.Invoke(LowerMaskItem);
		    onIndexChanged.Invoke(UpperMaskItem);
		    CurrentIndex = newIndex;
	    }

	    public override void RotateRadial(float rotation)
	    {
		    TotalRotation -= rotation;

		    ChangeIndex(Math.Min(MaxIndex, (int)(TotalRotation / ItemArcAngle)));

		    if (TotalRotation >= MaxIndexAngle)
		    {
			    FixedAngle(new Vector3(0, 0, 360 - CurrentIndex * ItemArcAngle));
		    }
		    else if (TotalRotation <= 0)
		    {
			    FixedAngle(Vector3.zero);
		    }
		    else
		    {
				RotationParent.Rotate(Vector3.forward, rotation);
				RotateMasks(Math.Min(Math.Max(rotation, -ItemArcAngle), ItemArcAngle));
		    }

	    }

	    private void FixedAngle(Vector3 angle)
	    {
		    RotationParent.localEulerAngles = angle;
		    LowerMaskItem.transform.localEulerAngles = Vector3.zero;
		    UpperMaskItem.transform.localEulerAngles = Vector3.zero;
	    }

	    private void RotateMasks(float maskAngle)
	    {
		    LowerMaskItem.transform.Rotate(Vector3.forward, maskAngle);
		    UpperMaskItem.transform.Rotate(Vector3.forward, maskAngle);
	    }

	    protected override (float lowerBound, float upperBound) GetItemBounds(int index, float itemRotation)
	    {
		    var lowerBound = itemRotation;
		    var upperBound = itemRotation + ItemArcAngle;
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
