using System;
using Cysharp.Threading.Tasks;
using Logs;
using Shared.Managers;
using US13.Core.Initialisation;
using US13.Managers.UpdateManager;
using US13.PlayerPrefs;

namespace US13.Core.Database
{
	public partial class ServerData : SingletonManager<ServerData>, IInitialise
	{
		public static Action serverDataLoaded;
		public string idToken;


		private bool fetchingToken = false;
		public static string IdToken => Instance.idToken;

		private void OnEnable()
		{
			UpdateManager.Add(UpdateMe, 10f);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
		}

		public InitialisationSystems Subsystem => InitialisationSystems.ServerData;

		void IInitialise.Initialise()
		{
			//Handles config for RCON and Server Status API for dedicated servers
			AttemptConfigLoad();
			AttemptRulesLoad();
			LoadMotd();

			serverDataLoaded?.Invoke();
		}

		private void UpdateMe()
		{
			if (config == null)
			{
				Loggy.Warning("Cannot update server status because server config is missing.", Category.DatabaseAPI);
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
				return;
			}

			if (buildInfo == null)
			{
				Loggy.Warning("Cannot update server status because build info is missing.", Category.DatabaseAPI);
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
				return;
			}

			if (string.IsNullOrEmpty(config.ServerToken))
			{
				Loggy.Warning(
					"No server token configured. This server won't post status updates to the server list",
					Category.DatabaseAPI);
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, UpdateMe);
				return;
			}

			UpdateManager.Add(UpdateMe, 10f);
			SendServerStatus().Forget();
		}

		/// <summary>
		///     Refresh the users profile data
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
			UnityEngine.PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountEmail);
			UnityEngine.PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountToken);
			UnityEngine.PlayerPrefs.SetInt("autoLogin", 0); // TODO remove these,
			UnityEngine.PlayerPrefs.Save();
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
