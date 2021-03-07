using System;
using System.Collections.Generic;
using UnityEngine;

public class AdminManager : MonoBehaviour
{
	public ItemStorage LocalAdminGhostStorage;
	public GameObject ItemStorageHolderPrefab;
	private Dictionary<string, ItemStorage> ghostStorageList = new Dictionary<string, ItemStorage>();

	private static AdminManager adminManager;
	public static AdminManager Instance => adminManager;

	private void Awake()
	{
		if (adminManager == null)
		{
			adminManager = this;
		}
		else
		{
			Destroy(this);
		}
	}

	private ItemStorage CreateItemSlotStorage(ConnectedPlayer player)
	{
		var holder = Spawn.ServerPrefab(ItemStorageHolderPrefab, null, gameObject.transform);
		var itemStorage = holder.GameObject.GetComponent<ItemStorage>();
		ghostStorageList.Add(player.UserId, itemStorage);
		return itemStorage;
	}

	public ItemStorage GetItemSlotStorage(ConnectedPlayer player)
	{
		if (ghostStorageList.ContainsKey(player.UserId))
		{
			return ghostStorageList[player.UserId];
		}
		return CreateItemSlotStorage(player);
	}
}
