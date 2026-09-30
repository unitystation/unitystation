using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using US13.Systems.Inventory;
using US13.UI.Core.NtOS;

namespace US13.Systems.NtOS.Core
{
	public class NtOS_Device : NetworkBehaviour
	{
		public List<INtOSModule> Modules { get; private set; } = new List<INtOSModule>();
		[SyncVar] public bool IsTurnedOn = false;
		[SyncVar] private List<StringBuilder> entries = new();

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

		public void ExecuteCommand(string command, NtOS_OutputEntry newLabelToUse)
		{
			var args = command.Split(' ');
			var commandName = args[0];
			args = args.Skip(1).ToArray();
			bool success = false;
			foreach (INtOSModule module in Modules)
			{
				if (commandName.ToLower().Contains(module.CommandName.ToLower()))
				{
					module.Execute(newLabelToUse.NetworkedText, args, this);
					success = true;
					break;
				}
			}

			if (success != true)
			{
				newLabelToUse.NetworkedText.SetValue($"Command [{commandName}] does not exist.");
			}
		}
	}
}