﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using Firebase.Extensions;
using Initialisation;
using UnityEngine;

namespace DatabaseAPI
{
	public partial class ServerData : MonoBehaviour, IInitialise
	{
		class Status
		{
			public bool error = false;
			public bool profileSet = false;
			public bool charReceived = false;
		}

		private static ServerData serverData;

		public static ServerData Instance
		{
			get
			{
				if (serverData == null)
				{
					serverData = FindObjectOfType<ServerData>();
				}

				return serverData;
			}
		}

		public static string UserFirestoreURL
		{
			get
			{
				return "https://firestore.googleapis.com/v1/projects/" +
				       $"unitystation-c6a53/databases/(default)/documents/users/{Auth.CurrentUser.UserId}";
			}
		}

		private Firebase.Auth.FirebaseAuth auth;
		public static Firebase.Auth.FirebaseAuth Auth => Instance.auth;

		private Dictionary<string, Firebase.Auth.FirebaseUser> userByAuth =
			new Dictionary<string, Firebase.Auth.FirebaseUser>();

		private Firebase.Auth.FirebaseUser user = null;

		public static string UserID
		{
			get
			{
				if (Instance.user == null)
				{
					return "";
				}

				return Instance.user.UserId;
			}
		}

		private bool fetchingToken = false;
		public string idToken;
		public static string IdToken => Instance.idToken;
		private HttpClient httpClient = new HttpClient();

		public static HttpClient HttpClient => Instance.httpClient;

		public InitialisationSystems Subsystem => InitialisationSystems.ServerData;

		void IInitialise.Initialise()
		{
			//Handles config for RCON and Server Status API for dedicated servers
			AttemptConfigLoad();
			InitializeFirebase();
		}

		// Handle initialization of the necessary firebase modules:
		protected void InitializeFirebase()
		{
			auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
			auth.StateChanged += AuthStateChanged;
			auth.IdTokenChanged += IdTokenChanged;
			AuthStateChanged(this, null);
		}

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.LoggedOut, OnLogOut);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.LoggedOut, OnLogOut);
		}

		void Update()
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
					Logger.LogError("Error with profile reload", Category.DatabaseAPI);
					return;
				}
			});
		}

		// Track state changes of the auth object.
		void AuthStateChanged(object sender, System.EventArgs eventArgs)
		{
			Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
			if (senderAuth != null) userByAuth.TryGetValue(senderAuth.App.Name, out user);
			if (senderAuth == auth && senderAuth.CurrentUser != user)
			{
				bool signedIn = user != senderAuth.CurrentUser && senderAuth.CurrentUser != null;
				if (!signedIn && user != null)
				{
					Logger.Log("Signed out ", Category.DatabaseAPI);
				}

				user = senderAuth.CurrentUser;
				userByAuth[senderAuth.App.Name] = user;
			}
		}

		// Track ID token changes.
		void IdTokenChanged(object sender, System.EventArgs eventArgs)
		{
			Firebase.Auth.FirebaseAuth senderAuth = sender as Firebase.Auth.FirebaseAuth;
			if (senderAuth == auth && senderAuth.CurrentUser != null && !fetchingToken)
			{
				senderAuth.CurrentUser.TokenAsync(false).ContinueWithOnMainThread(
					task => SetToken(task.Result));
			}
		}

		void SetToken(string result)
		{
			Instance.idToken = result;
		}

		public void OnLogOut()
		{
			auth.SignOut();
			idToken = "";
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
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
