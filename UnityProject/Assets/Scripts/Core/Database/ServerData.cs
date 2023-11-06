using System;
using Initialisation;
using Shared.Managers;
using UnityEngine;
using Logs;
using Shared.Util;
using Util;

namespace DatabaseAPI
{
	public partial class ServerData : SingletonManager<ServerData>, IInitialise
	{
		public static Action serverDataLoaded;


		private bool fetchingToken = false;
		public string idToken;
		public static string IdToken => Instance.idToken;

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

		/// <summary>
		/// Refresh the users profile data
		/// </summary>
		// public static void ReloadProfile()
		// {
		// 	ServerData.Auth.CurrentUser.ReloadAsync().ContinueWith(task =>
		// 	{
		// 		if (task.IsFaulted)
		// 		{
		// 			Loggy.LogError("Error with profile reload", Category.DatabaseAPI);
		// 			return;
		// 		}
		// 	});
		// }


		public void OnLogOut()
		{
			//auth.SignOut();
			idToken = "";
			//PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountUsername);
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountEmail);
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountToken);
			PlayerPrefs.SetInt("autoLogin", 0); // TODO remove these,
			PlayerPrefs.Save();
		}
	}

	[Serializable]
	public class RefreshToken
	{
		public string refreshToken;
		public string userID;
	}

	[Serializable]
	public class FireStoreResponse
	{
		public FireStoreError error;
		public string name;
		public FireStoreFields fields;
	}

	[Serializable]
	public class FireStoreError
	{
		public ushort code;
		public string message;
		public string status;
	}

	[Serializable]
	public class FireStoreFields
	{
		public FireStoreCharacter character;
	}

	[Serializable]
	public class FireStoreCharacter
	{
		public string stringValue;
	}
}
