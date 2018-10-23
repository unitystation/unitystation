using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
	public Image dragDummy;
	private bool isDragging = false;
	public UI_ItemSlot ItemSlotCache { get; private set; }
	public GameObject ItemCache { get; private set; }

	public void Start()
	{
		dragDummy.enabled = false;
	}
	public void UI_ItemDrag(UI_ItemSlot itemSlot)
	{
		if (itemSlot.Item != null)
		{
			ItemSlotCache = itemSlot;
			isDragging = true;
			dragDummy.enabled = true;
			dragDummy.sprite = itemSlot.image.sprite;
			itemSlot.image.enabled = false;
			ItemCache = itemSlot.Item;
		}
	}

	public void StopDrag()
	{
		isDragging = false;
		dragDummy.enabled = false;
		if (ItemSlotCache != null)
		{
			if (ItemSlotCache.Item != null)
			{
				ItemSlotCache.image.enabled = true;
			}
		}
		ItemSlotCache = null;
		ItemCache = null;
	}

	public void Update()
	{
		if (isDragging)
		{
			dragDummy.transform.position = Input.mousePosition;
		}
	}
}