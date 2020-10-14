using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UI.Core.Radial;
using UI.Core.Events;

namespace UI.Core.RightClick
{
	public class RightClickMenuController : MonoBehaviour
	{
		[SerializeField]
		private RightClickOption examineOption = default;

		[SerializeField]
		private RightClickOption pullOption = default;

		[SerializeField]
		private RightClickOption pickUpOption = default;

		[SerializeField]
	    private RadialBranch radialBranch = default;

	    [SerializeField]
	    private ItemRadial itemRadialPrefab = default;

	    [SerializeField]
	    private ActionRadial actionRadialPrefab = default;

	    [SerializeField]
	    private AnimationCurve snapCurve = default;

	    [SerializeField]
	    private float snapTime = default;

	    private ItemRadial itemRadial;

	    private ActionRadial actionRadial;

	    private List<RightClickMenuItem> Items { get; set; }

	    private float SnapStartTime { get; set; }

	    private float SnapRotation { get; set; }

	    private ItemRadial ItemRadial
	    {
		    get
		    {
			    if (itemRadial != null)
			    {
				    return itemRadial;
			    }

			    itemRadial = Instantiate(itemRadialPrefab, transform);
			    var radialDrag = itemRadial.GetComponent<RadialDrag>();
			    if (radialDrag != null)
			    {
				    radialDrag.OnBeginDragEvent = OnBeginDragEvent;
					radialDrag.OnEndDragEvent = OnEndDragEvent;
			    }
			    var radialScrollWheel = itemRadial.GetComponent<RadialMouseWheelScroll>();
			    if (radialScrollWheel != null)
			    {
				    radialScrollWheel.OnEndScrollEvent = OnEndScrollEvent;
			    }
			    itemRadial.OnIndexChanged = OnIndexChanged;
			    itemRadial.RadialEvents.AddListener(PointerEventType.PointerEnter, OnHoverItem);
			    itemRadial.RadialEvents.AddListener(PointerEventType.PointerClick, OnClickItem);
			    return itemRadial;
		    }
	    }

	    private ActionRadial ActionRadial
	    {
		    get
		    {
			    if (actionRadial != null)
			    {
				    return actionRadial;
			    }

			    actionRadial = Instantiate(actionRadialPrefab, transform);
			    actionRadial.SetConstraintSource(ItemRadial.RotationParent);
			    actionRadial.RadialEvents.AddListener(PointerEventType.PointerClick, OnClickAction);
			    return actionRadial;
		    }
	    }

	    public void SetupMenu(List<RightClickMenuItem> items, Vector3 position, bool followWorldPosition)
	    {
		    this.SetActive(true);
		    Items = items;
		    radialBranch.Setup(position, itemRadialPrefab.OuterRadius, itemRadialPrefab.Scale, followWorldPosition);
		    ItemRadial.Setup(items.Count);
		    foreach (var item in ItemRadial)
		    {
			    var index = item.Index;
			    if (index < items.Count)
			    {
				    item.ChangeItem(items[index]);
			    }
		    }
		    ItemRadial.transform.localPosition = radialBranch.MenuPosition;

		    ActionRadial.Setup(0);
		    ActionRadial.SetActive(false);
	    }

	    private void OnBeginDragEvent(PointerEventData pointerEvent)
	    {
		    ItemRadial.Selected.OrNull()?.FadeOut(pointerEvent);
		    ItemRadial.ChangeLabel(string.Empty);
		    ActionRadial.SetActive(false);
		    SnapRotation = 0;
		    foreach (var button in ItemRadial)
		    {
			    button.Interactable = false;
		    }
	    }

	    private void OnEndDragEvent(PointerEventData pointerEvent)
	    {
		    foreach (var button in ItemRadial)
		    {
			    button.Interactable = true;
		    }
		    SnapRotation = ItemRadial.NearestItemAngle;
		    SnapStartTime = Time.time;
		    var item = ItemRadial.Selected;
		    if (item != null && item.IsRaycastLocationValid(pointerEvent.position, null))
		    {
			    pointerEvent.dragging = false;
			    item.OnPointerEnter(pointerEvent);
		    }
		    else
		    {
			    ItemRadial.Selected = null;
		    }
	    }

	    private void OnEndScrollEvent(PointerEventData pointerEvent)
	    {
		    var item = ItemRadial.Selected;
		    if (item == null)
		    {
			    return;
		    }

		    ItemRadial.ChangeLabel(string.Empty);
		    ActionRadial.SetActive(false);
		    SnapRotation = ItemRadial.NearestItemAngle;
		    SnapStartTime = Time.time;
		    if (item.IsRaycastLocationValid(pointerEvent.position, null))
		    {
			    pointerEvent.scrollDelta = Vector2.zero;
			    item.OnPointerEnter(pointerEvent);
		    }
		    else
		    {
			    item.ResetState();
			    ItemRadial.Selected = null;
		    }
	    }

	    private void OnIndexChanged(RightClickRadialButton button)
	    {
		    var index = button.Index;
		    if (index > Items.Count)
		    {
			    return;
		    }
		    var itemInfo = Items[index];
		    button.ChangeItem(itemInfo);
	    }

	    private void OnHoverItem(PointerEventData eventData, RightClickRadialButton button)
	    {
		    var index = button.Index;
		    if (eventData.dragging || index > Items.Count)
		    {
			    return;
		    }

		    var item = Items[index];
		    ItemRadial.ChangeLabel(item.Label);
		    var actions = item.SubMenus;
		    if (actions == null)
		    {
			    ActionRadial.SetActive(false);
			    return;
		    }

		    ActionRadial.SetActive(true);
		    ActionRadial.Setup(actions.Count);
		    for (var i = 0; i < actions.Count; i++)
		    {
			    ActionRadial[i].ChangeItem(actions[i]);
		    }

		    ActionRadial.UpdateRotation(index % ItemRadial.ShownItemsCount, ItemRadial);
	    }

	    private void OnClickItem(PointerEventData eventData, RightClickRadialButton button)
	    {
		    var subItems = Items[button.Index]?.SubMenus;

		    if (subItems == null)
		    {
			    return;
		    }

		    // Copern: Not a preferable method of doing this but the original RightClickMenuItem wasn't really
		    // designed for this.
		    if (KeyboardInputManager.IsShiftPressed())
		    {
			    DoAction(examineOption);
		    }
		    else if (KeyboardInputManager.IsControlPressed())
		    {
			    DoAction(pullOption);
		    }
		    else
		    {
			    DoAction(pickUpOption);
		    }

		    void DoAction(RightClickOption option)
		    {
			    foreach (var item in subItems)
			    {
				    if (item.Label != option.label)
				    {
					    continue;
				    }
				    item.Action();
				    this.SetActive(option.keepMenuOpen);
				    return;
			    }
		    }
	    }

	    private void OnClickAction(PointerEventData eventData, RightClickRadialButton button)
	    {
		    if (ItemRadial.Selected == null)
		    {
			    return;
		    }

		    var itemIndex = ItemRadial.Selected.Index;
		    var actionIndex = button.Index;
		    var actionMenu = Items[itemIndex]?.SubMenus?[actionIndex];
		    if (actionMenu == null)
		    {
			    return;
		    }
		    actionMenu.Action();
		    this.SetActive(actionMenu.keepMenuOpen);
	    }

	    public void Update()
	    {
		    SnapToNearestItem();
		    radialBranch.UpdateDirection();
		    ItemRadial.transform.localPosition = radialBranch.MenuPosition;
		    ItemRadial.UpdateArrows();

		    if (IsAnyPointerDown() == false)
		    {
			    return;
		    }

		    // Deactivate the menu if there was a mouse click outside of the menu.
		    var mousePos = CommonInput.mousePosition;
		    if (ItemRadial.IsPositionWithinRadial(mousePos, true) == false && ActionRadial.IsPositionWithinRadial(mousePos) == false)
		    {
			    this.SetActive(false);
		    }
	    }

	    private void SnapToNearestItem()
	    {
		    // Snap Rotation is set to zero on drag. The equality check here is to keep it from rotating while dragging.
		    if (SnapRotation.Equals(0))
		    {
			    return;
		    }

		    var currentSnapTime = Time.time - SnapStartTime;
		    var eval = currentSnapTime > 0 ? currentSnapTime / snapTime : 0;
		    var change = SnapRotation * snapCurve.Evaluate(eval);
		    ItemRadial.RotateRadial(change);
		    if (currentSnapTime <= snapTime)
		    {
			    SnapRotation -= change;
		    }
		    else
		    {
			    SnapRotation = 0;
		    }
	    }

	    private bool IsAnyPointerDown()
	    {
		    for (var i = 0; i < 3; i++)
		    {
			    if (CommonInput.GetMouseButtonDown(i))
			    {
				    return true;
			    }
		    }

		    return false;
	    }
	}
}
