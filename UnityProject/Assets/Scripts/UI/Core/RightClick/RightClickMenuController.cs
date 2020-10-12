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
			    radialDrag.OrNull()?.OnBeginDragEvent.AddListener(OnBeginDragEvent);
			    radialDrag.OrNull()?.OnEndDragEvent.AddListener(OnEndDragEvent);
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

	    private float SnapStartTime { get; set; }

	    private float SnapRotation { get; set; }

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
		    ItemRadial.Selected.OrNull()?.OnDeselect(pointerEvent);
		    ItemRadial.ChangeLabel(string.Empty);
		    ActionRadial.SetActive(false);
		    SnapRotation = 0;
	    }

	    private void OnEndDragEvent(PointerEventData pointerEvent)
	    {
		    SnapRotation = ItemRadial.NearestItemAngle;
		    SnapStartTime = Time.time;
	    }

	    private void OnIndexChanged(RightClickRadialButton button)
	    {
		    ItemRadial.ChangeLabel(string.Empty);
		    ActionRadial.SetActive(false);
		    var itemInfo = Items[button.Index];
		    button.ChangeItem(itemInfo);
	    }

	    private void OnHoverItem(PointerEventData eventData, RightClickRadialButton button)
	    {
		    if (eventData.dragging)
		    {
			    return;
		    }

		    var index = button.Index;
		    var item = Items[index];
		    ItemRadial.ChangeLabel(item?.Label);
		    var actions = item?.SubMenus;
		    if (item == null || actions == null)
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
		    var currentSnapTime = Time.time - SnapStartTime;

		    // Snap Rotation is set to zero on drag. The inequality check here is to keep it from rotating after dragging.
		    if (!SnapRotation.Equals(0) && currentSnapTime <= snapTime)
		    {
			    var eval = currentSnapTime > 0 ? currentSnapTime / snapTime : 0;
			    var change = SnapRotation * snapCurve.Evaluate(eval);
			    ItemRadial.RotateRadial(change);
			    SnapRotation -= change;
		    }
		    else if (currentSnapTime > snapTime)
		    {
			    ItemRadial.RotateRadial(SnapRotation);
			    SnapRotation = 0;
		    }

		    radialBranch.UpdateDirection();
		    ItemRadial.transform.localPosition = radialBranch.MenuPosition;
		    ItemRadial.UpdateArrows();

		    if (!IsAnyPointerDown())
		    {
			    return;
		    }

		    // Deactivate the menu if there was a mouse click outside of the menu.
		    var mousePos = CommonInput.mousePosition;
		    if (!ItemRadial.IsPositionWithinRadial(mousePos, true) && !ActionRadial.IsPositionWithinRadial(mousePos))
		    {
			    this.SetActive(false);
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
