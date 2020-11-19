using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
				itemRadial.Drag.OnBeginDragEvent = OnBeginDragEvent;
				itemRadial.Drag.OnEndDragEvent = _ => ItemRadial.TweenRotation(ItemRadial.NearestItemAngle);
				itemRadial.MouseWheelScroll.OnScrollEvent = OnScrollEvent;
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
				actionRadial.RadialEvents.AddListener(PointerEventType.PointerExit, (eventData, button) => button.FadeOut(eventData));
				return actionRadial;
			}
		}

		public void SetupMenu(List<RightClickMenuItem> items, Vector3 worldPosition, bool followWorldPosition)
		{
			Items = items;
			radialBranch.Setup(worldPosition, itemRadialPrefab.OuterRadius, itemRadialPrefab.Scale, followWorldPosition);
			ItemRadial.SetupWithItems(items);
			ItemRadial.transform.localPosition = radialBranch.MenuPosition;
			UpdateItemRadialRotation();

			this.SetActive(true);
			ItemRadial.SetActive(true);

			ActionRadial.Setup(0);
			ActionRadial.SetActive(false);
		}

		private void OnBeginDragEvent(PointerEventData pointerEvent)
		{
			ItemRadial.Selected.OrNull()?.FadeOut(pointerEvent);
			ItemRadial.SetItemsInteractable(false);
			ActionRadial.SetActive(false);
		}

		private void OnScrollEvent(PointerEventData pointerEvent)
		{
			if (ItemRadial.TryBeginRotation(pointerEvent.scrollDelta.y))
			{
				ActionRadial.SetActive(false);
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
			var itemRadialPosition = ItemRadial.transform.position;
			var actions = item.SubMenus;
			if (actions == null)
			{
				ActionRadial.SetActive(false);
				radialBranch.UpdateLineSize(ActionRadial, itemRadialPosition);
				return;
			}

			ActionRadial.SetupWithActions(actions);
			ActionRadial.UpdateRotation(index, ItemRadial.ItemArcMeasure);
			radialBranch.UpdateLineSize(ActionRadial, itemRadialPosition);
		}

		private void OnClickItem(PointerEventData eventData, RightClickRadialButton button)
		{
			var subItems = Items[button.Index]?.SubMenus;

			if (subItems == null)
			{
				return;
			}

			// Copern: Not a preferable method of doing this but the original RightClickMenuItem wasn't really
			// designed for this. Also need to switch this to use keybinds.
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
			radialBranch.UpdateDirection();
			ItemRadial.transform.localPosition = radialBranch.MenuPosition;
			ItemRadial.UpdateArrows();
			radialBranch.UpdateLineSize(ActionRadial, ItemRadial.transform.position);
			UpdateItemRadialRotation();

			if (IsAnyPointerDown() == false)
			{
				return;
			}

			// Deactivate the menu if there was a mouse click outside of the menu.
			var mousePos = CommonInput.mousePosition;
			if (ItemRadial.IsPositionWithinRadial(mousePos) == false && ActionRadial.IsPositionWithinRadial(mousePos) == false)
			{
				this.SetActive(false);
			}
		}

		private void UpdateItemRadialRotation()
		{
			var angle = radialBranch.PositionToBranchAngle(ItemRadial.transform.position);
			angle -= Math.Min(Items.Count, ItemRadial.MaxShownItems) * ItemRadial.ItemArcMeasure / 2;
			ItemRadial.transform.localEulerAngles = new Vector3(0, 0, angle);
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

		private void OnDisable()
		{
			// These need to be disabled for the next time the controller is reactivated
			ItemRadial.SetActive(false);
			ActionRadial.SetActive(false);
		}
	}
}
