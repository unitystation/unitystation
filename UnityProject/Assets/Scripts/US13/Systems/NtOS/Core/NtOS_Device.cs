using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public struct OutputEntry
		{
			public int Id;
			public StringBuilder Text;
		}

		public Action OnModulesRefreshed;

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
			itemStorage = GetComponent<ItemStorage>();
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
			Modules.AddRange(GetComponentsInChildren<INtOSModule>());
			Modules.AddRange(GetComponent<CondensedNtModules>().ModulesToAdd);
			Modules.AddRange(GetComponentInChildren<CondensedNtModules>().ModulesToAdd);
			if (itemStorage == null) return;
			foreach (ItemSlot slot in itemStorage.GetOccupiedSlots())
			{
				if (slot == null || slot.ItemObject == null) continue;
				Modules.AddRange(slot.ItemObject.GetComponents<INtOSModule>());
				Modules.AddRange(slot.ItemObject.GetComponentsInChildren<INtOSModule>());
				Modules.AddRange(slot.ItemObject.GetComponent<CondensedNtModules>().ModulesToAdd);
				Modules.AddRange(slot.ItemObject.GetComponentInChildren<CondensedNtModules>().ModulesToAdd);
			}
		}

		[Command(requiresAuthority = false)]
		public void ExecuteCommand(string command)
		{
			//todo: validation
			var args = command.Split(' ');
			var commandName = args[0];
			args = args.Skip(1).ToArray();
			bool success = false;
			foreach (INtOSModule module in Modules)
			{
				if (commandName.ToLower().Contains(module.CommandName.ToLower()) == false) continue;
				var builder = new StringBuilder();
				module.Execute(History.Count + 1, args, this, builder);
				success = true;
				History.Add( new OutputEntry
				{
					Id = History.Count + 1,
					Text = builder
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