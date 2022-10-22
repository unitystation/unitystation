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
				item.Key.IsActive = item.Value;
			}
		}

		public void EnableModule(PublicTerminalModule module)
		{
			module.IsActive = true;
		}

		public void DisableModule(PublicTerminalModule module)
		{
			module.IsActive = false;
		}
	}
}