using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class GUI_Vendor : NetTab
{
	[SerializeField]
	private bool allowSell = true;
	[SerializeField]
	private float cooldownTimer = 2f;

	private Vendor vendor;
	private List<VendorItem> vendorContent = new List<VendorItem>();
	[SerializeField]
	private EmptyItemList itemList = null;
	[SerializeField]
	private NetColorChanger hullColor = null;
	private bool inited = false;
	[SerializeField]
	private string deniedMessage = "Bzzt.";
	[SerializeField]
	private string restockMessage = "Items restocked.";

	private void Start()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}
	}

	protected override void InitServer()
	{
		StartCoroutine(WaitForProvider());
	}

	IEnumerator WaitForProvider()
	{
		while (Provider == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		vendor = Provider.GetComponent<Vendor>();
		hullColor.SetValueServer(vendor.HullColor);
		inited = true;
		GenerateContentList();
		UpdateAllItems();
		vendor.OnRestockUsed.AddListener(RestockItems);
	}

	private void GenerateContentList()
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		vendorContent = new List<VendorItem>();
		for (int i = 0; i < vendor.VendorContent.Count; i++)
		{
			//protects against missing references
			if (vendor.VendorContent[i] != null && vendor.VendorContent[i].Item != null)
			{
				vendorContent.Add(new VendorItem(vendor.VendorContent[i]));
			}
		}
	}

	public override void OnEnable()
	{
		base.OnEnable();
		if (!CustomNetworkManager.Instance._isServer || !inited)
		{
			return;
		}
		UpdateAllItems();
		allowSell = true;
	}

	public void RestockItems()
	{
		GenerateContentList();
		UpdateAllItems();
		SendToChat(restockMessage);
	}

	/// <summary>
	/// Clear all items and send new vendor state to clients UI
	/// </summary>
	private void UpdateAllItems()
	{
		itemList.Clear();
		itemList.AddItems(vendorContent.Count);
		for (int i = 0; i < vendorContent.Count; i++)
		{
			VendorItemEntry item = itemList.Entries[i] as VendorItemEntry;
			item.SetItem(vendorContent[i], this);
		}
	}

	/// <summary>
	/// Updates only single item in vendor UI
	/// </summary>
	private void UpdateItem(VendorItem itemToUpdate)
	{
		// find entry for this item
		var vendorItems = itemList.Entries;
		var vendorItemEntry = vendorItems.FirstOrDefault((listEntry) =>
		{
			var itemEntry = listEntry as VendorItemEntry;
			return itemEntry && (itemEntry.vendorItem == itemToUpdate);
		}) as VendorItemEntry;

		if (!vendorItemEntry)
		{
			Logger.LogError($"Can't find {itemToUpdate} to update in {this.gameObject} vendor. " +
				$"UpdateAllItems wasn't called before?", Category.UI);
			return;
		}

		vendorItemEntry.SetItem(itemToUpdate, this);
	}

	public void VendItem(VendorItem item)
	{
		if (item == null || vendor == null) return;
		VendorItem itemToSpawn = null;
		foreach (var vendorItem in vendorContent)
		{
			if (vendorItem == item)
			{
				itemToSpawn = item;
				break;
			}
		}

		if (!CanSell(itemToSpawn))
		{
			return;
		}

		Vector3 spawnPos = vendor.gameObject.RegisterTile().WorldPositionServer;
		var spawnedItem = Spawn.ServerPrefab(itemToSpawn.Item, spawnPos, vendor.transform.parent).GameObject;
		//something went wrong trying to spawn the item
		if (spawnedItem == null) return;

		itemToSpawn.Stock--;

		var itemNameStr = TextUtils.UppercaseFirst(spawnedItem.ExpensiveName());
		SendToChat($"{itemNameStr} was dispensed from the vending machine");

		//Ejecting in direction
		if (vendor.EjectObjects && vendor.EjectDirection != EjectDirection.None &&
		    spawnedItem.TryGetComponent<CustomNetTransform>(out var cnt))
		{
			Vector3 offset = Vector3.zero;
			switch (vendor.EjectDirection)
			{
				case EjectDirection.Up:
					offset = vendor.transform.rotation * Vector3.up / Random.Range(4, 12);
					break;
				case EjectDirection.Down:
					offset = vendor.transform.rotation * Vector3.down / Random.Range(4, 12);
					break;
				case EjectDirection.Random:
					offset = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(-0.15f, 0.15f), 0);
					break;
			}
			cnt.Throw(new ThrowInfo
			{
				ThrownBy = spawnedItem,
				Aim = BodyPartType.Chest,
				OriginWorldPos = spawnPos,
				WorldTrajectory = offset,
				SpinMode = (vendor.EjectDirection == EjectDirection.Random) ? SpinMode.Clockwise : SpinMode.None
			});
		}

		UpdateItem(item);
		allowSell = false;
		StartCoroutine(VendorInputCoolDown());
	}

	private bool CanSell(VendorItem itemToSpawn)
	{
		if (allowSell && itemToSpawn != null && itemToSpawn.Stock > 0)
		{
			return true;
		}
		SendToChat(deniedMessage);
		return false;
	}

	private void SendToChat(string messageToSend)
	{
		Chat.AddLocalMsgToChat(messageToSend, vendor.transform.position, vendor?.gameObject);
	}

	private IEnumerator VendorInputCoolDown()
	{
		yield return WaitFor.Seconds(cooldownTimer);
		allowSell = true;
	}
}
