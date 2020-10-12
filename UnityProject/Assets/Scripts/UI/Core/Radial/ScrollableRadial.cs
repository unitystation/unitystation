using System;
using UnityEngine;
using UnityEngine.Events;

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

	    protected T LowerMaskItem => InitOrGetMaskItem(ref lowerMaskItem, lowerMask, 0);

	    protected T UpperMaskItem => InitOrGetMaskItem(ref upperMaskItem, upperMask, 1);

	    protected int MaxIndex => TotalItemCount - ShownItemsCount;

	    protected float MaxIndexAngle => MaxIndex * ItemArcMeasure;

	    private T InitOrGetMaskItem(ref T maskItem, RectTransform parent, int index)
	    {
		    InitItem(index);
		    if (maskItem == null)
		    {
			    maskItem = Items[index];
			    maskItem.transform.SetParent(parent);
		    }

		    return maskItem;
	    }

	    public override void Setup(int itemCount)
	    {
		    BasicSetup(itemCount);
		    TotalRotation = 0;
		    CurrentIndex = 0;

		    var isScrollable = itemCount > ShownItemsCount;
		    upperMask.localEulerAngles = new Vector3(0, 0, ArcMeasure - 360);
		    SetupItem(LowerMaskItem, 0, Vector3.zero, isScrollable);
		    SetupItem(UpperMaskItem, ShownItemsCount, new Vector3(0, 0 , ShownItemsCount * ItemArcMeasure), isScrollable);
		    for (var i = 2; i < Math.Max(2 + ShownItemsCount, Count); i++)
		    {
			    InitItem(i);
			    var index = isScrollable ? i - 1 : i - 2;
			    var rotation = new Vector3(0, 0, index * ItemArcMeasure);
			    var isActive = index < ShownItemsCount;
			    SetupItem(Items[i], index, rotation, isActive);
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
			    var unfilled = Mathf.Sign(delta) * (ArcMeasure - 360);
			    var posDelta = Math.Abs(delta);
			    var rotation = angle * (1 + posDelta / shownItems) + unfilled;
			    // Rotate the changed unmasked items around the masks and set their new index
			    for (var i = 0; i < Math.Min(posDelta, shownItems - 1); i++)
			    {
				    var itemIndex = (startIndex + i) % (shownItems - 1) + 2;
				    Items[itemIndex].transform.Rotate(Vector3.back, rotation);
				    Items[itemIndex].Index = startIndex + i + indexModifier;
				    OnIndexChanged?.Invoke(Items[itemIndex]);
			    }
		    }

		    float rotate;

		    if (delta > 0)
		    {
			    rotate = ItemArcMeasure;
			    ChangeUnmaskedIndices(rotate, CurrentIndex, shownItems + delta / shownItems);
		    }
		    else
		    {
			    rotate = -ItemArcMeasure;
			    ChangeUnmaskedIndices(rotate, newIndex, 1);
		    }

		    LowerMaskItem.Index = newIndex;
		    UpperMaskItem.Index = (newIndex + shownItems) % TotalItemCount;
		    RotateMasks(rotate);
		    OnIndexChanged?.Invoke(LowerMaskItem);
		    OnIndexChanged?.Invoke(UpperMaskItem);
		    CurrentIndex = newIndex;
	    }

	    public override void RotateRadial(float rotation)
	    {
		    if (TotalRotation <= 0 && rotation >= 0 || TotalRotation >= MaxIndexAngle && rotation <= 0)
		    {
			    return;
		    }

		    TotalRotation -= rotation;

		    ChangeIndex(Math.Min(MaxIndex, (int)(TotalRotation / ItemArcMeasure)));

		    if (TotalRotation >= MaxIndexAngle)
		    {
			    FixedAngle(new Vector3(0, 0, 360 - CurrentIndex * ItemArcMeasure));
		    }
		    else if (TotalRotation <= 0)
		    {
			    FixedAngle(Vector3.zero);
		    }
		    else
		    {
				RotationParent.Rotate(Vector3.forward, rotation);
				RotateMasks(Math.Min(Math.Max(rotation, -ItemArcMeasure), ItemArcMeasure));
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
