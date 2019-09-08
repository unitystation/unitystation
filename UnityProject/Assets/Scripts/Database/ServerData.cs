using System;
using System.Collections;
using System.Collections.Generic;
using Firebase;
using Firebase.Extensions;
using Lobby;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
	public partial class ServerData : MonoBehaviour
	{
		class Status { public bool error = false; public bool profileSet = false; public bool charReceived = false; }

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

		private const string FirebaseRoot = "https://firestore.googleapis.com/v1/projects/unitystation-c6a53/databases/(default)/documents";
		private Firebase.Auth.FirebaseAuth auth;
		public static Firebase.Auth.FirebaseAuth Auth => Instance.auth;
		private Dictionary<string, Firebase.Auth.FirebaseUser> userByAuth = new Dictionary<string, Firebase.Auth.FirebaseUser>();
		private Firebase.Auth.FirebaseUser user = null;
		private bool fetchingToken = false;
		public string token;
		public string refreshToken;
		public bool isFirstTime = false;

		void Awake()
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
			if (connectedToHub)
			{
				MonitorServerStatus();
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
					Debug.LogError("Error with profile reload");
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
				if (signedIn)
				{
					//TODO: Display name stuff
					/* 
					displayName = user.DisplayName ?? "";
					DisplayDetailedUserInfo(user, 1);
					*/
				}
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
			if (string.IsNullOrEmpty(token))
			{
				Instance.token = result;
			}
			else
			{
				Instance.token = result;
				Instance.refreshToken = result;
			}

			if (isFirstTime)
			{
				isFirstTime = false;
				UpdateCharacterProfile(PlayerManager.CurrentCharacterSettings, NewCharacterSuccess, NewCharacterFailed);
			}
		}

		void NewCharacterSuccess(string msg) { }

		void NewCharacterFailed(string msg) { }

		public void OnLogOut()
		{
			auth.SignOut();
			token = "";
			refreshToken = "";
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
			PlayerPrefs.Save();
		}
	}
}