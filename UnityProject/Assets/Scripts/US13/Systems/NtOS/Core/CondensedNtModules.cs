using System.Collections.Generic;
using UnityEngine;
using US13.Core.Attributes;

namespace US13.Systems.NtOS.Core
{
	[RequireComponent(typeof(NtOS_Device))]
	public class CondensedNtModules : MonoBehaviour
	{
		private NtOS_Device device;

		[SerializeReference, SelectImplementation(typeof(INtOSModule))]
		public List<INtOSModule> ModulesToAdd = new List<INtOSModule>();

		private void Awake()
		{
			device = GetComponent<NtOS_Device>();
			device.OnModulesRefreshed += AddListOfModules;
		}

		private void OnDestroy()
		{
			if(device) device.OnModulesRefreshed -= AddListOfModules;
		}

		private void AddListOfModules()
		{
			foreach (var module in ModulesToAdd)
			{
				if (ModulesToAdd.Contains(module)) continue;
				device.Modules.Add(module);
			}
		}
	}
}