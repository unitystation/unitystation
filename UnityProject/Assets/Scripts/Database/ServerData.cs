using System;
using System.Collections;
using System.Collections.Generic;
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
		private const string ApiKey = "77bCwycyzm4wJY5X"; //preloaded for development. Keys are replaced on the server
		private const string URL_TryCreate = ServerRoot + "/create?key=" + ApiKey + "&data=";
		private const string URL_TryLogin = ServerRoot + "/login?key=" + ApiKey + "&data=";
		private const string URL_UpdateChar = ServerRoot + "/updatechar?key=" + ApiKey + "&data=";
		private const string URL_GetChar = ServerRoot + "/getchar?key=" + ApiKey + "&username=";

		void Start()
		{
			if (PlayerPrefs.HasKey("autoLogin"))
			{
				int getLoginSetting = PlayerPrefs.GetInt("autoLogin");
				if (getLoginSetting == 1)
				{
					sessionCookie = PlayerPrefs.GetString("cookie");
					TryRetrieveCharSettings(OnAutoLoginSuccess, OnAutoLoginFailure);
					if (LobbyManager.Instance != null)
					{
						LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
					}
				}
			}
		}

		private void OnAutoLoginSuccess(string msg)
		{
			GameData.IsLoggedIn = true;
			GameData.LoggedInUsername = PlayerPrefs.GetString("username");
			PlayerManager.CurrentCharacterSettings = JsonUtility.FromJson<CharacterSettings>(msg);
			PlayerPrefs.SetString("currentcharacter", msg);
			if (LobbyManager.Instance != null)
			{
				LobbyManager.Instance.lobbyDialogue.ShowConnectionPanel();
			}
		}

		private void OnAutoLoginFailure(string msg)
		{
			//Log out on any error for the moment:
			GameData.IsLoggedIn = false;
			Logger.LogError(msg, Category.DatabaseAPI);
		}

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.LoggedOut, OnLogOut);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.LoggedOut, OnLogOut);
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