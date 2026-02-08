using System.Collections.Generic;
using System.Linq;
using Shared.Managers;
using UnityEngine;
using US13.Systems.Inventory;
using US13.UI.Systems.MainHUD.UI_Bottom;

namespace US13.UI.Systems.AdminTools.DevTools
{
	public class InventoryViewerDynamicManager : SingletonManager<InventoryViewerDynamicManager>
	{
		public GameObject Todebug;
		public UI_StorageHandler prefab;

		public static IEnumerable<ItemStorage> TraverseInventories(ItemStorage RootStorage, bool ShowOnlyOccupied = false)
		{
			if (RootStorage == null) return null;
			var Storages = RootStorage.GetItemStorageTree();

			if (ShowOnlyOccupied)
			{
				Storages = Storages.Where(x => x.HasAnyOccupied());
			}


			return Storages;
		}


		public void LoadInventories(List<ItemStorage> InStorages)
		{
			int i = -3;
			float j = -2.2f;
			float yOffset = -180f; // Adjust this value as needed
			float xOffset = 660f; // Adjust this value as needed


			foreach (var Storage in InStorages)
			{
				var UI_StorageHandler = Instantiate(prefab, this.transform);

				// Apply offset to position
				RectTransform rt = UI_StorageHandler.GetComponent<RectTransform>();
				if (rt != null)
				{
					rt.anchoredPosition +=
						new Vector2(j * xOffset, i * yOffset); // Vertical offset; change x for horizontal
				}

				UI_StorageHandler.OpenStorageUI(Storage, true);
				i++;
				if (i > 3)
				{
					i = -3;
					j++;
				}
			}
		}
	}
}