using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Logs;
using Mirror;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Attributes;
using US13.Systems.Inventory;

namespace US13.Systems.NtOS.Core
{
	public class NtOS_Device : NetworkBehaviour
	{
		public List<INtOSModule> Modules { get; private set; } = new List<INtOSModule>();

		[SerializeReference, SelectImplementation(typeof(INtOSModule)), ShowIf(nameof(runWelcomeModuleOnStart))]
		public INtOSModule StartingModule;

		private bool runWelcomeModuleOnStart = true;

		[SyncVar] public bool IsTurnedOn = false;
		[SyncVar] public List<OutputEntry> History = new();

		private ItemStorage itemStorage;

		public class OutputEntry
		{
			public int Id;
			public StringBuilder Text;
			public bool Locked;
		}

		public Action OnModulesRefreshed;

		private void Awake()
		{
			itemStorage = GetComponent<ItemStorage>();
			if (itemStorage != null)
			{
				itemStorage.ServerInventoryItemSlotSet  += OnInventoryChanged;
			}
		}

		private void Start()
		{
			RefreshModules();
			var welcomeText = new StringBuilder();
			StartingModule.Execute(History.Count + 1, Array.Empty<string>(), this, welcomeText);
			History.Add(new OutputEntry
			{
				Id = History.Count + 1,
				Text = welcomeText
			});
		}

		private void OnInventoryChanged(Pickupable oldItem, Pickupable newItem)
		{
			RefreshModules();
		}

		/// <summary>
		/// Grabs all modules that can be hosted on the device itself, or via an item module.
		/// Use this sparingly as it does a lot of GetComponent checks on itself, its children, and item storage if available.
		/// </summary>
		private void RefreshModules()
		{
			Modules.Clear();
			// support embedding modules directly on devices instead of via dedicated items
			// so players don't mess with important functions.
			Modules.AddRange(GetComponents<INtOSModule>());
			Modules.AddRange(GetComponent<CondensedNtModules>().ModulesToAdd);
			if (itemStorage == null) return;
			foreach (ItemSlot slot in itemStorage.GetOccupiedSlots())
			{
				if (slot == null || slot.ItemObject == null) continue;
				Modules.AddRange(slot.ItemObject.GetComponents<INtOSModule>());
				List<INtOSModule> rootModules = slot.ItemObject.GetComponent<CondensedNtModules>()?.ModulesToAdd;
				if (rootModules != null) Modules.AddRange(rootModules);
			}
		}

		public void ServerClearHistory()
		{
			History.RemoveAll(x => x.Locked == false);
		}

		[Command(requiresAuthority = false)]
		public void ExecuteCommand(string command)
		{
			//todo: validation
			var args = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var commandName = args[0];
			args = args.Skip(1).ToArray();
			bool success = false;
			foreach (INtOSModule module in Modules)
			{
				if (module == null)
				{
					Loggy.Warning("Null module found on nt device. Make sure to clear them up!!");
					continue;
				}
				if (module.CommandName == string.Empty) continue;
				if (string.Equals(commandName, module.CommandName, StringComparison.OrdinalIgnoreCase) == false) continue;
				var builder = new StringBuilder();
				module.Execute(History.Count + 1, args, this, builder);
				success = true;
				History.Add( new OutputEntry
				{
					Id = History.Count + 1,
					Text = builder,
					Locked = false
				});
				break;
			}
			if (success != true)
			{
				History.Add( new OutputEntry
				{
					Id = History.Count + 1,
					Text = new StringBuilder().AppendLine($"Couldn't find [{commandName}] command.")
				});
			}
		}
	}
}