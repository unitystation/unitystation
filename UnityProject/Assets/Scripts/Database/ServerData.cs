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

		private string sessionCookie;
		private const string ServerRoot = "https://dev.unitystation.org"; //dev mode (todo: load release url and key data through build server)
		private const string FirebaseRoot = "https://firestore.googleapis.com/v1/projects/unitystation-c6a53/databases/(default)/documents";
		private const string ApiKey = "77bCwycyzm4wJY5X"; //preloaded for development. Keys are replaced on the server
		private const string URL_TryCreate = ServerRoot + "/create?key=" + ApiKey + "&data=";
		private const string URL_TryCreateChar = FirebaseRoot + "/users/";
		//private const string URL_UpdateChar = ServerRoot + "/updatechar?key=" + ApiKey + "&data=";
		private const string URL_UpdateChar = ServerRoot + "/updatechar?key=" + ApiKey + "&data=";
		private const string URL_GetChar = ServerRoot + "/getchar?key=" + ApiKey + "&username=";

		private Firebase.Auth.FirebaseAuth auth;
		private Dictionary<string, Firebase.Auth.FirebaseUser> userByAuth = new Dictionary<string, Firebase.Auth.FirebaseUser>();
		private Firebase.Auth.FirebaseUser user = null;
		private bool fetchingToken = false;
		public string token;
		public bool isFirstTime = false;

		void Start()
		{
			InitializeFirebase();
		}

		// Handle initialization of the necessary firebase modules:
		protected void InitializeFirebase()
		{
			Debug.Log("Setting up Firebase Auth");
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
					Debug.Log("Signed out " + user.UserId);
				}
				user = senderAuth.CurrentUser;
				userByAuth[senderAuth.App.Name] = user;
				if (signedIn)
				{
					Debug.Log("Signed in " + user.UserId);
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
			Instance.token = result;
			if (isFirstTime)
			{
				isFirstTime = false;
				UpdateCharacterProfile(PlayerManager.CurrentCharacterSettings, NewCharacterSuccess, NewCharacterFailed);
			}
		}

		void NewCharacterSuccess(string msg)
		{

		}

		void NewCharacterFailed(string msg)
		{

		}

		public void OnLogOut()
		{
			sessionCookie = null;
			PlayerPrefs.SetString("username", "");
			PlayerPrefs.SetString("cookie", "");
			PlayerPrefs.SetInt("autoLogin", 0);
			PlayerPrefs.Save();
		}
		//Example of request with cookie auth
		// IEnumerator AttemptTest(string request)
		// {
		// 	UnityWebRequest r = UnityWebRequest.Get(ServerRoot + "/test?data=" + WWW.EscapeURL(request));
		// 	r.SetRequestHeader("Cookie", sessionCookie);

		// 	yield return r.SendWebRequest();
		// 	if (r.error != null)
		// 	{
		// 		Logger.Log("DB request failed: " + r.error, Category.DatabaseAPI);
		// 	} else {

		// 	}
		// }
	}

	[Serializable]
	public class RequestCreateAccount
	{
		public string username;
		public string password;
		public string email;
		public string apiKey;
	}

	[Serializable]
	public class RequestLogin
	{
		public string username;
		public string password;
		public string apiKey;
	}

	[Serializable]
	public class ApiResponse
	{
		public int errorCode; //0 = all good, read the message variable now, otherwise read errorMsg
		public string errorMsg;
		public string message;
	}
}