using System;
using System.Collections.Generic;
using UnityEngine;
using Doors;
using Items;
using UnityEngine.EventSystems;
using UI.Core.Events;
using System.Threading.Tasks;

namespace UI.Core.RightClick
{
	public class RadialMenu : MonoBehaviour
	{
		public static readonly Color ButtonColor = new Color(0.3f, 0.55f, 0.72f, 0.7f);

		private static readonly BranchWorldPosition BranchWorldPosition = new BranchWorldPosition();

		[SerializeField]
		private RadialBranch radialBranch = default;

		[SerializeField]
		private ItemRadial itemRadialPrefab = default;

		private ItemRadial itemRadial;

		private List<RightClickMenuItem> Items { get; set; }

		private GameObject selectedObject;

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
				itemRadial.Drag.OnEndDragEvent = OnEndDragEvent;
				itemRadial.Scroll.OnScrollEvent = OnScrollEvent;
				itemRadial.Scroll.OnKeyEvent = OnScrollEvent;
				itemRadial.OnIndexChanged = OnIndexChanged;
				itemRadial.RadialEvents.AddListener(PointerEventType.PointerEnter, OnHoverItem);
				itemRadial.RadialEvents.AddListener(PointerEventType.PointerClick, OnClickItem);
				return itemRadial;
			}
		}

		public async Task<GameObject> ShowRadialMenu(List<GameObject> objects, GameObject radialCenter)
		{
			this.SetActive(true);
			ItemRadial.SetActive(true);
			selectedObject = default;   //reset previous selection
			Items = GenerateItemMenu(objects);
			IBranchPosition branchPosition = SetCenter(radialCenter);
			radialBranch.SetupAndEnable((RectTransform)ItemRadial.transform, ItemRadial.OuterRadius, ItemRadial.Scale, branchPosition);
			ItemRadial.SetupWithItems(Items);
			ItemRadial.CenterItemsTowardsAngle(Items.Count, radialBranch.GetBranchToTargetAngle());
			while (selectedObject == null && isActiveAndEnabled == true)
			{
				await Task.Yield(); //wait until the selectedObject is chosen
			}
			return selectedObject;
		}

		private List<RightClickMenuItem> GenerateItemMenu(List<GameObject> objects)
		{
			if (objects == null || objects.Count == 0)
			{
				return null;
			}
			var result = new List<RightClickMenuItem>();
			foreach (var curObject in objects)
			{
				result.Add(CreateObjectMenu(curObject));
			}
			return result;
		}

		private RightClickMenuItem CreateObjectMenu(GameObject forObject)
		{
			var label = forObject.ExpensiveName();

			// check if is a paletted item
			ItemAttributesV2 item = forObject.GetComponent<ItemAttributesV2>();
			List<Color> palette = null;
			if (item != null)
			{
				if (item.ItemSprites.IsPaletted)
				{
					palette = item.ItemSprites.Palette;
				}
			}

			// See if this object has an AirLockAnimator then try to get the sprite from that, otherwise try to get the sprite from the first renderer we find
			var airLockAnimator = forObject.GetComponentInChildren<AirLockAnimator>();
			var spriteRenderer = airLockAnimator != null ? airLockAnimator.doorbase : forObject.GetComponentInChildren<SpriteRenderer>();

			Sprite sprite = null;
			if (spriteRenderer != null)
			{
				sprite = spriteRenderer.sprite;
			}
			else
			{
				Logger.LogWarningFormat("Could not determine sprite to use for right click menu" +
						" for object {0}. Please manually configure a sprite in a RightClickAppearance component" +
						" on this object.", Category.UserInput, forObject.name);
			}

			return RightClickMenuItem.CreateObjectMenuItem(forObject, ButtonColor, sprite, null, label, spriteRenderer.color, palette);
		}

		private IBranchPosition SetCenter(GameObject radialCenter)
		{
			var tile = radialCenter.RegisterTile();
			return BranchWorldPosition.SetTile(tile);
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
		}

		private void OnClickItem(PointerEventData eventData, RightClickRadialButton button)
		{
			var choice = Items[button.Index].gameObject;

			if (choice == null)
			{
				return;
			}
			selectedObject = choice;
			this.SetActive(false);
		}

		private void OnBeginDragEvent(PointerEventData pointerEvent)
		{
			ItemRadial.Selected.OrNull()?.FadeOut(pointerEvent);
			ItemRadial.SetItemsInteractable(false);
			ItemRadial.Scroll.enabled = false;
		}

		private void OnEndDragEvent(PointerEventData pointerEvent)
		{
			ItemRadial.TweenRotation(ItemRadial.NearestItemAngle);
			ItemRadial.Scroll.enabled = true;
		}

		private void OnScrollEvent(PointerEventData pointerEvent)
		{
			ItemRadial.TryBeginRotation(pointerEvent.scrollDelta.y);
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

		public void Update()
		{
			ItemRadial.UpdateArrows();
			if (radialBranch.PositionChanged())
			{
				this.SetActive(false);
				return;
			}

			if (IsAnyPointerDown() == false)
			{
				return;
			}

			// Deactivate the menu if there was a mouse click outside of the menu.
			var mousePos = CommonInput.mousePosition;
			if (ItemRadial.IsPositionWithinRadial(mousePos) == false)
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

		private void OnDisable()
		{
			// These need to be disabled for the next time the controller is reactivated
			this.SetActive(false);
			ItemRadial.SetActive(false);
			radialBranch.SetActive(false);
		}
	}
}

