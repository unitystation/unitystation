using System;
using System.Collections.Generic;
using System.Net.Http;
using Firebase.Extensions;
using Initialisation;
using UnityEngine;
using Firebase.Auth;
using Logs;
using Shared.Util;
using Util;

namespace DatabaseAPI
{
	public partial class ServerData : MonoBehaviour, IInitialise
	{
		private class Status
		{
			public bool error = false;
			public bool profileSet = false;
			public bool charReceived = false;
		}

		private static ServerData serverData;

		public static ServerData Instance => FindUtils.LazyFindObject(ref serverData);

		public static string UserFirestoreURL => "https://firestore.googleapis.com/v1/projects/" +
				$"unitystation-c6a53/databases/(default)/documents/users/{Auth.CurrentUser.UserId}";

		private FirebaseAuth auth;
		public static FirebaseAuth Auth => Instance.OrNull()?.auth;

		private readonly Dictionary<string, FirebaseUser> userByAuth = new();

		private FirebaseUser user = null;

		public static string UserID => Instance.user == null
				? string.Empty
				: Instance.user.UserId;

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
			InitializeFirebase();

			serverDataLoaded?.Invoke();
		}

		// Handle initialization of the necessary firebase modules:
		protected void InitializeFirebase()
		{
			auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
			auth.StateChanged += AuthStateChanged;
			auth.IdTokenChanged += IdTokenChanged;
			AuthStateChanged(this, null);
		}

		private void OnEnable()
		{
			EventManager.AddHandler(Event.AccountLoggedOut, OnLogOut);
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			EventManager.RemoveHandler(Event.AccountLoggedOut, OnLogOut);
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
		public static void ReloadProfile()
		{
			ServerData.Auth.CurrentUser.ReloadAsync().ContinueWith(task =>
			{
				if (task.IsFaulted)
				{
					Loggy.LogError("Error with profile reload", Category.DatabaseAPI);
					return;
				}
			});
		}

		// Track state changes of the auth object.
		private void AuthStateChanged(object sender, EventArgs eventArgs)
		{
			Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
			if (senderAuth != null) userByAuth.TryGetValue(senderAuth.App.Name, out user);
			if (senderAuth == auth && senderAuth.CurrentUser != user)
			{
				bool signedIn = user != senderAuth.CurrentUser && senderAuth.CurrentUser != null;
				if (!signedIn && user != null)
				{
					Loggy.Log("Signed out ", Category.DatabaseAPI);
				}

				user = senderAuth.CurrentUser;
				userByAuth[senderAuth.App.Name] = user;
			}
		}

		// Track ID token changes.
		private void IdTokenChanged(object sender, EventArgs eventArgs)
		{
			Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
			if (senderAuth == auth && senderAuth.CurrentUser != null && !fetchingToken)
			{
				senderAuth.CurrentUser.TokenAsync(false).ContinueWithOnMainThread(
					task => SetToken(task.Result));
			}
		}

		private void SetToken(string result)
		{
			Instance.idToken = result;
		}

		public void OnLogOut()
		{
			auth.SignOut();
			idToken = "";
			PlayerPrefs.DeleteKey(PlayerPrefKeys.AccountUsername);
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
