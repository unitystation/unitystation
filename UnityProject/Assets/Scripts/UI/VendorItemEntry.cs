using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VendorItemEntry : DynamicEntry
{
	[SerializeField]
	private Color regularColor = Color.gray;
	[SerializeField]
	private Color emptyStockColor = Color.red;
	[HideInInspector]
	public VendorItem vendorItem;
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

		var itemGO = vendorItem.Item;
		var itemAttr = itemGO.GetComponent<ItemAttributesV2>();

		// try get human-readable item name
		var itemNameStr = TextUtils.UppercaseFirst(itemGO.ExpensiveName());
		itemName.SetValueServer(itemNameStr);

		itemIcon.SetValueServer(itemGO.name);

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

	public void OnVendItemButtonPressed(ConnectedPlayer player)
	{
		if (vendorItem == null || vendorWindow == null)
		{
			return;
		}

		vendorWindow.OnVendItemButtonPressed(vendorItem, player);
	}
}
