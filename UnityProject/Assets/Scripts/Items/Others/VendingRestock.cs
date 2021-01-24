using Objects;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VendingRestock : Pickupable
{	
	private List<VendorItem> previousVendorContent;
	/// <summary>
	/// The items previously offered by a deconstructed vendor
	/// </summary>
	public List<VendorItem> PreviousVendorContent => previousVendorContent;

	public void SetPreviousVendorContent(List<VendorItem> items)
	{
		previousVendorContent = items;
	}
}
