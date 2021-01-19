using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;
using Objects;

namespace UI.Objects
{
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

			if (itemGO != null)
			{
				// TODO This is unused. What was it for? Is this why soda machine entries are just called Drinking glass? (Issue #4942)
				// I've just moved the line around to stop the NRE.
				var itemAttr = itemGO.GetComponent<ItemAttributesV2>();
			}
			else
			{
				Logger.LogError($"{this} variable {nameof(itemGO)} was null!");
			}

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
}
