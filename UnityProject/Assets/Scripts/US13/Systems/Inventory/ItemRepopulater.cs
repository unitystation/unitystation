using System;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers.UpdateManager;
using US13.Systems.Inventory;
using Util;

namespace Storage
{
	public class ItemRepopulater : MonoBehaviour
	{
		public float SecondsToRepopulate = 0;
		public int ItemSlotindex = 0;
		public GameObject PrefabToPopulateWith;

		private ItemSlot itemSlot;

		public void Start()
		{
			itemSlot = this.GetCachedComponent<ItemStorage>().GetIndexedItemSlot(ItemSlotindex);
		}

		public void OnEnable()
		{
			UpdateManager.Add(UpdateMe, SecondsToRepopulate);
		}

		public void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public void UpdateMe()
		{
			if (itemSlot.IsEmpty == false) return;
			var Item = Spawn.ServerPrefab(PrefabToPopulateWith);
			Inventory.ServerAdd(Item.GameObject, itemSlot);
		}
	}
}