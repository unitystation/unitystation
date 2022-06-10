using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
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
		[SerializeField]
		private NetLabel priceTag;

		public void SetItem(VendorItem item, GUI_Vendor correspondingWindow)
		{
			vendorItem = item;
			vendorWindow = correspondingWindow;

			var itemGO = vendorItem.Item;
			// try get human-readable item name
			var itemNameStr = TextUtils.UppercaseFirst(item.ItemName);

			itemName.SetValueServer(itemNameStr);
			itemIcon.SetValueServer(itemGO.name);
			itemCount.SetValueServer($"({vendorItem.Stock})");
			itemBackground.SetValueServer(vendorItem.Stock > 0 ? regularColor : emptyStockColor);

			if (vendorItem.Price == 0)
			{
				priceTag.SetValueServer("Free");
			}
			else
			{
				priceTag.SetValueServer(vendorItem.Currency == CurrencyType.Credits
						? $"{vendorItem.Price} cr"
						: $"{vendorItem.Price} Points");
			}
		}

		public void OnVendItemButtonPressed(PlayerInfo player)
		{
			if (vendorItem == null || vendorWindow == null) return;

			vendorWindow.OnVendItemButtonPressed(vendorItem, player);
		}
	}
}
