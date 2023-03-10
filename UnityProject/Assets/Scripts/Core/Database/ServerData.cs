using System;
using Initialisation;
using Shared.Managers;

namespace DatabaseAPI
{
	public partial class ServerData : SingletonManager<ServerData>, IInitialise
	{
		public static Action serverDataLoaded;

		public InitialisationSystems Subsystem => InitialisationSystems.ServerData;

		void IInitialise.Initialise()
		{
			//Handles config for RCON and Server Status API for dedicated servers
			AttemptConfigLoad();
			AttemptRulesLoad();
			LoadMotd();

			serverDataLoaded?.Invoke();
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (config != null)
			{
				if (!string.IsNullOrEmpty(config.HubUser) && !string.IsNullOrEmpty(config.HubPass))
				{
					MonitorServerStatus();
				}
			}
		}
	}
}
