using System;
using System.Collections.Generic;
using Mirror;
using US13.Systems.Inventory;

namespace US13.Systems.NtOS.Core
{
	public class NtOS_Device : NetworkBehaviour
	{
		public List<INtOSModule> Modules { get; private set; } = new List<INtOSModule>();
		[SyncVar] public bool IsTurnedOn = false;

		public Action OnModulesRefreshed;

		private void Start()
		{
			RefreshModules();
		}

		private void RefreshModules()
		{
			Modules.Clear();
			Modules.AddRange(GetComponents<INtOSModule>());
			Modules.AddRange(GetComponentsInChildren<INtOSModule>());
			if (TryGetComponent<ItemStorage>(out var itemStorage) == false) return;
			foreach (var slot in itemStorage.GetOccupiedSlots())
			{
				if (slot == null || slot.ItemObject == null) continue;
				Modules.AddRange(slot.ItemObject.GetComponents<INtOSModule>());
				Modules.AddRange(slot.ItemObject.GetComponentsInChildren<INtOSModule>());
			}
		}
	}
}