using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VendorItemEntry : DynamicEntry
{
	[SerializeField]
	private Color regularColor = Color.gray;
	[SerializeField]
	private Color emptyStockColor = Color.red;
	private VendorItem vendorItem;
	private GUI_Vendor vendorWindow;
	[SerializeField]
	private NetLabel itemName = null;
	[SerializeField]
	private NetLabel itemCount = null;
	[SerializeField]
	private NetPrefabImage itemIcon = null;
	[SerializeField]
	private NetColorChanger itemBackground = null;

	public void SetItem(VendorItem item, GUI_Vendor correspondingWindow)
	{
		vendorItem = item;
		vendorWindow = correspondingWindow;
		itemName.SetValueServer(vendorItem.Item.name);
		itemIcon.SetValueServer(vendorItem.Item.name);
		itemCount.SetValueServer($"({vendorItem.Stock.ToString()})");
		if (vendorItem.Stock <= 0)
		{
			itemBackground.SetValueServer(emptyStockColor);
		}
		else
		{
			itemBackground.SetValueServer(regularColor);
		}
	}

	public void VendorItem()
	{
		if (vendorItem == null || vendorWindow == null)
		{
			return;
		}
		vendorWindow.VendItem(vendorItem);
	}
}
