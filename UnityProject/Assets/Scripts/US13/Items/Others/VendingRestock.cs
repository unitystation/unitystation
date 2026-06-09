using System.Collections.Generic;
using US13.Objects;
using US13.Systems.Inventory;

namespace US13.Items.Others
{
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
}
