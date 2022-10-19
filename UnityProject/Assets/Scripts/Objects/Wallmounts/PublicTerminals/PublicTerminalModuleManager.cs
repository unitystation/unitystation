using System;
using Mirror;
using UnityEngine;

namespace Objects.Wallmounts.PublicTerminals
{
	public class PublicTerminalModuleManager : MonoBehaviour
	{
		[SerializeField]
		private SerializableDictionary<PublicTerminalModule, bool>
			enabledModules = new SerializableDictionary<PublicTerminalModule, bool>();

		private void Awake()
		{
			foreach (var item in enabledModules)
			{
				item.Key.isActive = item.Value;
			}
		}

		public void EnableModule(PublicTerminalModule module)
		{
			module.isActive = true;
		}

		public void DisableModule(PublicTerminalModule module)
		{
			module.isActive = false;
		}
	}
}