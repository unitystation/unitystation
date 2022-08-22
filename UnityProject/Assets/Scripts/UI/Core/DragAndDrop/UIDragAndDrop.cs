using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	/// <summary>
	/// Drag and drop component, for use in the UI only. Renamed from DragAndDrop to avoid conflict
	/// with AssetUsageDetector plugin
	/// </summary>
	public class UIDragAndDrop : MonoBehaviour
	{
		public Image dragDummy;
		private bool isDragging = false;
		public UI_ItemSlot FromSlotCache { get; private set; }
		public GameObject DraggedItem { get; private set; }
		public Shadow shadow;

		Vector3 scaleCache;
		Vector3 interactableScale;

		// during a drag and drop, whether we dropped something into a slot (thus shouldn't
		// check for interactions in the world)

		/// <summary>
		/// Indicate that the current drag and drop has resulted in something being dropped on a UI slot or
		/// an interaction happening, thus
		/// no MouseDrop interactions should be checked against things in the world under the mouse.
		/// </summary>
		[NonSerialized]
		public bool DropInteracted;

		public void Start()
		{
			dragDummy.enabled = false;
			scaleCache = dragDummy.transform.localScale;
			interactableScale = scaleCache * 1.1f;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		public void UI_ItemDrag(UI_ItemSlot fromSlot)
		{
			if (fromSlot.Item != null && !isDragging)
			{
				DropInteracted = false;
				FromSlotCache = fromSlot;
				isDragging = true;
				dragDummy.enabled = true;
				dragDummy.sprite = fromSlot.Image.MainSprite;
				fromSlot.Clear();
				DraggedItem = fromSlot.ItemObject;
			}
		}

		public void StopDrag()
		{
			if (!DropInteracted && DraggedItem != null)
			{
				var mouseDrops = DraggedItem.GetComponents<IBaseInteractable<MouseDrop>>();
				// check for MouseDrop interactions in the world if we didn't drop on a UI slot
				// check what we dropped on, which may or may not have mousedrop interaction components
				var dropTargets =
					MouseUtils.GetOrderedObjectsUnderMouse();

				// go through the stack of objects and call any drop components we find
				foreach (GameObject dropTarget in dropTargets)
				{
					MouseDrop info = MouseDrop.ByLocalPlayer(DraggedItem, dropTarget.gameObject);
					// call this object's mousedrop interaction methods if it has any, for each object we are dropping on
					if (InteractionUtils.ClientCheckAndTrigger(mouseDrops, info) != null) break;
					var targetComps = dropTarget.GetComponents<IBaseInteractable<MouseDrop>>()
						.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
					if (InteractionUtils.ClientCheckAndTrigger(targetComps, info) != null) break;
				}
			}
			DropInteracted = false;
			isDragging = false;
			dragDummy.enabled = false;
			if (FromSlotCache != null)
			{
				if (FromSlotCache.Item != null)
				{
					FromSlotCache.RefreshImage();
				}
			}


			FromSlotCache = null;
			DraggedItem = null;
			ResetInteractable();
		}

		public void EnteredInteractable()
		{
			dragDummy.transform.localScale = interactableScale;
			if (dragDummy.enabled)
			{
				shadow.enabled = true;
			}
		}

		public void ResetInteractable()
		{
			dragDummy.transform.localScale = scaleCache;
			shadow.enabled = false;
		}

		public void UpdateMe()
		{
			if (isDragging)
			{
				dragDummy.transform.position = CommonInput.mousePosition;
			}
		}
	}
}
