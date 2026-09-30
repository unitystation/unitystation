using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using UnityEngine;
using US13.Core.Attributes;
using US13.Systems.Inventory;

namespace US13.Systems.NtOS.Core
{
	public class NtOS_Device : NetworkBehaviour
	{
		public List<INtOSModule> Modules { get; private set; } = new List<INtOSModule>();

		[SerializeReference, SelectImplementation(typeof(INtOSModule))]
		public INtOSModule StartingModule;

		[SyncVar] public bool IsTurnedOn = false;
		[SyncVar] public List<OutputEntry> History = new();

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

		[Command(requiresAuthority = false)]
		public void ExecuteCommand(string command)
		{
			var args = command.Split(' ');
			var commandName = args[0];
			args = args.Skip(1).ToArray();
			bool success = false;
			foreach (INtOSModule module in Modules)
			{
				if (commandName.ToLower().Contains(module.CommandName.ToLower()))
				{
					var builder = new StringBuilder();
					module.Execute(History.Count + 1, args, this, new StringBuilder());
					success = true;
					History.Add( new OutputEntry
					{
						Id = History.Count + 1,
						Text = builder
					});
					break;
				}
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