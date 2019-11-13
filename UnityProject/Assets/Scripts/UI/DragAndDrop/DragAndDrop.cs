using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drag and drop component, for use in the UI only.
/// </summary>
public class DragAndDrop : MonoBehaviour
{
	public Image dragDummy;
	private bool isDragging = false;
	public UI_ItemSlot ItemSlotCache { get; private set; }
	public GameObject ItemCache { get; private set; }
	public Shadow shadow;

	Vector3 scaleCache;
	Vector3 interactableScale;

	//during a drag and drop, whether we dropped something into a slot (thus shouldn't
	//check for interactions in the world)

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
	public void UI_ItemDrag(UI_ItemSlot itemSlot)
	{
		if (itemSlot.Item != null && !isDragging)
		{
			DropInteracted = false;
			ItemSlotCache = itemSlot;
			isDragging = true;
			dragDummy.enabled = true;
			dragDummy.sprite = itemSlot.Image.sprite;
			itemSlot.Clear();
			ItemCache = itemSlot.ItemObject;
		}
	}

	public void StopDrag()
	{
		if (!DropInteracted && ItemCache != null)
		{
			var mouseDrops = ItemCache.GetComponents<IBaseInteractable<MouseDrop>>();
			//check for MouseDrop interactions in the world if we didn't drop on a UI slot
			//check what we dropped on, which may or may not have mousedrop interaction components
			var dropTargets =
				MouseUtils.GetOrderedObjectsUnderMouse();

			//go through the stack of objects and call any drop components we find
			foreach (GameObject dropTarget in dropTargets)
			{
				MouseDrop info = MouseDrop.ByLocalPlayer( ItemCache, dropTarget.gameObject);
				//call this object's mousedrop interaction methods if it has any, for each object we are dropping on
				if (InteractionUtils.ClientCheckAndTrigger(mouseDrops, info) != null) break;
				var targetComps = dropTarget.GetComponents<IBaseInteractable<MouseDrop>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(targetComps, info) != null) break;
			}
		}
		DropInteracted = false;
		isDragging = false;
		dragDummy.enabled = false;
		if (ItemSlotCache != null)
		{
			if (ItemSlotCache.Item != null)
			{
				ItemSlotCache.RefreshImage();
			}
		}


		ItemSlotCache = null;
		ItemCache = null;
		ResetInteractable();
	}

	public void EnteredInteractable()
	{
		if(dragDummy.transform.localScale != interactableScale){
			dragDummy.transform.localScale = interactableScale;
		}
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

	public void Update()
	{
		if (isDragging)
		{
			dragDummy.transform.position = CommonInput.mousePosition;
		}
	}
}