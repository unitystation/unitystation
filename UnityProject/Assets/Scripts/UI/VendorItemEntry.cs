using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VendorItemEntry : DynamicEntry
{
	private VendorItem vendorItem;
	private GUI_Vendor vendorWindow;

	public void SetItem(VendorItem item, GUI_Vendor correspondingWindow)
	{
		vendorItem = item;
		vendorWindow = correspondingWindow;
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
