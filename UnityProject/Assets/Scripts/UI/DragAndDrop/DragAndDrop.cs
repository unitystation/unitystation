using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
	public Image dragDummy;
	private bool isDragging = false;
	private UI_ItemSlot itemSlotCache;

	public void Start()
	{
		dragDummy.enabled = false;
	}
	public void UI_ItemDrag(UI_ItemSlot itemSlot)
	{
		if (itemSlot.Item != null)
		{
			itemSlotCache = itemSlot;
			isDragging = true;
			dragDummy.enabled = true;
			dragDummy.sprite = itemSlot.image.sprite;
			itemSlot.image.enabled = false;
		}
	}

	public void StopDrag()
	{
		isDragging = false;
		dragDummy.enabled = false;
		if(itemSlotCache != null){
			itemSlotCache.image.enabled = true;
		}
		itemSlotCache = null;
	}

	public void Update()
	{
		if (isDragging)
		{
			dragDummy.transform.position = Input.mousePosition;
		}
	}
}