using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GUI_Vendor : NetTab
{
	[SerializeField]
	private EmptyItemList itemList = null;
	[SerializeField]
	private NetColorChanger hullColor = null;

	private Vendor vendor;

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	private IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			// waiting for Provider
			yield return WaitFor.EndOfFrame;
		}

		vendor = Provider.GetComponent<Vendor>();
		if (vendor)
		{
			hullColor.SetValueServer(vendor.HullColor);
			UpdateAllItemsView();

			vendor.OnItemVended.AddListener(UpdateItemView);
			vendor.OnRestockUsed.AddListener(UpdateAllItemsView);
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (CustomNetworkManager.IsServer)
		{
			UpdateAllItemsView();
		}
	}

	/// <summary>
	/// Buy UI button was pressed by client
	/// </summary>
	public void OnVendItemButtonPressed(VendorItem vendorItem, ConnectedPlayer player)
	{
		if (vendor)
		{
			vendor.TryVendItem(vendorItem, player);
		}
	}

	/// <summary>
	/// Clear all items and send new vendor state to clients UI
	/// </summary>
	private void UpdateAllItemsView()
	{
		if (!vendor)
		{
			return;
		}

		// remove all items UI
		itemList.Clear();

		var vendorContent = vendor.VendorContent;
		itemList.AddItems(vendorContent.Count);

		// update UI for clients
		for (int i = 0; i < vendorContent.Count; i++)
		{
			VendorItemEntry item = itemList.Entries[i] as VendorItemEntry;
			item.SetItem(vendorContent[i], this);
		}
	}

	/// <summary>
	/// Update only single item and send new state to clients UI
	/// </summary>
	private void UpdateItemView(VendorItem itemToUpdate)
	{
		if (!vendor)
		{
			return;
		}
		if (!APCPoweredDevice.IsOn(vendor.ActualCurrentPowerState))  return;
		// find entry for this item
		var vendorItems = itemList.Entries;
		var vendorItemEntry = vendorItems.FirstOrDefault((listEntry) =>
		{
			var itemEntry = listEntry as VendorItemEntry;
			return itemEntry && (itemEntry.vendorItem == itemToUpdate);
		}) as VendorItemEntry;

		// check if found entry is valid
		if (!vendorItemEntry)
		{
			Logger.LogError($"Can't find {itemToUpdate} to update in {this.gameObject} vendor. " +
			                $"UpdateAllItems wasn't called before?", Category.UI);
			return;
		}

		// update entry UI state
		vendorItemEntry.SetItem(itemToUpdate, this);
	}
}
