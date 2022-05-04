using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
	public class AdminManager : SingletonManager<AdminManager>
	{
		public ItemStorage LocalAdminGhostStorage;
		public GameObject ItemStorageHolderPrefab;
		private Dictionary<string, ItemStorage> ghostStorageList = new Dictionary<string, ItemStorage>();

		private ItemStorage CreateItemSlotStorage(PlayerInfo player)
		{
			var holder = Spawn.ServerPrefab(ItemStorageHolderPrefab, null, gameObject.transform);
			var itemStorage = holder.GameObject.GetComponent<ItemStorage>();
			ghostStorageList.Add(player.UserId, itemStorage);
			return itemStorage;
		}

		public ItemStorage GetItemSlotStorage(PlayerInfo player)
		{
			if (ghostStorageList.ContainsKey(player.UserId))
			{
				return ghostStorageList[player.UserId];
			}
			return CreateItemSlotStorage(player);
		}
	}
}
