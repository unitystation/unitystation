using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityWebRequest = UnityEngine.Networking.UnityWebRequest;
using Mirror;
using System.Text.RegularExpressions;
using Core.Database.Models;
using IgnoranceTransport;
using Managers;

namespace DatabaseAPI
{
	/// <summary>
	/// If activated on a server then it will update the
	/// unitystation rest api with the status of this server.
	/// To gain access to the unitystation hub for your server
	/// speak to unitystation staff on discord.
	/// </summary>
	public partial class ServerData
	{
		private ServerPublicInfo publicInfo;
		/// <summary>
		/// Config model with the server's public info, like download links, server name, etc.
		/// </summary>
		public static ServerPublicInfo ServerPublicInfo => Instance.publicInfo;

		private BuildInfo buildInfo;

		private ServerSecrets secrets;
		public static ServerSecrets ServerSecrets => Instance.secrets;

		private string hubCookie;
		private const string hubRoot = "https://api.unitystation.org";
		private const string hubLogin = hubRoot + "/login?data=";
		private const string hubUpdate = hubRoot + "/statusupdate?data=";
		private float updateWait = 0f;
		private string publicIP;
		private TelepathyTransport telepathyTransport;
		private Ignorance ignoranceTransport;
		//private BoosterTransport boosterTransport = null;

		private void AttemptServerInfoLoad()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "serverInfo.json");
			buildInfo = JsonUtility.FromJson<BuildInfo>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath, "buildinfo.json")));

			if (File.Exists(path))
			{
				telepathyTransport = FindObjectOfType<TelepathyTransport>();
				ignoranceTransport = FindObjectOfType<Ignorance>();
				publicInfo = JsonUtility.FromJson<ServerPublicInfo>(File.ReadAllText(path));
				Instance.StartCoroutine(Instance.SendServerStatus());
			}
			else
			{
				Logger.Log("No config found for server information", Category.DatabaseAPI);
			}
		}

		//TODO: try to also get and parse current environmental variables to build the secrets object so the plain
		// file isn't needed by server admins
		private void AttemptServerSecretsLoad()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "serverSecrets.json");

			if (File.Exists(path))
			{
				secrets = JsonUtility.FromJson<ServerSecrets>(File.ReadAllText(path));
			}
			else
			{
				Logger.Log("No config found for Rcon and Server Hub connections", Category.DatabaseAPI);
			}
		}

		private void MonitorServerStatus()
		{
			updateWait += Time.deltaTime;
			//Update the hub every 10 seconds
			if (updateWait >= 10f)
			{
				updateWait = 0f;
				Instance.StartCoroutine(Instance.SendServerStatus());
			}
		}

		IEnumerator SendServerStatus()
		{
			if (string.IsNullOrEmpty(secrets.HubUser) || string.IsNullOrEmpty(secrets.HubPass))
			{
				Logger.Log("Invalid Hub creds found, aborting HUB connection", Category.DatabaseAPI);
				yield break;
			}

			var loginRequest = new HubLoginReq
			{
				username = secrets.HubUser,
				password = secrets.HubPass
			};

			var requestData = JsonUtility.ToJson(loginRequest);
			UnityWebRequest req = UnityWebRequest.Get(hubLogin + UnityWebRequest.EscapeURL(requestData));
			yield return req.SendWebRequest();
			if (req.error == null)
			{
				var response = JsonUtility.FromJson<ApiResponse>(req.downloadHandler.text);
				if (response.errorCode == 0)
				{
					string s = req.GetResponseHeader("set-cookie");
					hubCookie = s.Split(';') [0];
					req = UnityWebRequest.Get("http://ipinfo.io/ip");
					yield return req.SendWebRequest();
					publicIP = Regex.Replace(req.downloadHandler.text, @"\t|\n|\r", "");
				}
				else if (response.errorCode == 901)
				{
					Logger.Log("Hub API returned unauthorized credentials, aborting HUB connection", Category.DatabaseAPI);
					yield break;
				}
				else
				{
					Logger.Log("Hub API returned error code "+response.errorCode+", aborting HUB connection\n"+response.errorMsg, Category.DatabaseAPI);
					yield break;
				}
			}
			else
			{
				Logger.Log("Hub API returned error, aborting HUB connection", Category.DatabaseAPI);
				yield break;
			}

			var status = new ServerStatus();
			status.ServerName = publicInfo.ServerName;
			status.ForkName = buildInfo.ForkName;
			status.BuildVersion = buildInfo.BuildNumber;

			if (SubSceneManager.Instance == null)
			{
				status.CurrentMap = "loading";
			}
			else
			{
				status.CurrentMap = SubSceneManager.ServerChosenMainStation;
			}

			status.GameMode = GameManager.Instance.GetGameModeName();
			status.IngameTime = GameManager.Instance.roundTimer.text;
			if (PlayerList.Instance != null)
			{
				status.PlayerCount = PlayerList.Instance.ConnectionCount;
			}
			status.ServerIP = publicIP;
			status.ServerPort = GetPort();
			status.WinDownload = publicInfo.WinDownload;
			status.OSXDownload = publicInfo.OSXDownload;
			status.LinuxDownload = publicInfo.LinuxDownload;

			status.fps = (int)FPSMonitor.Instance.Current;

			UnityWebRequest r = UnityWebRequest.Get(hubUpdate + UnityWebRequest.EscapeURL(JsonUtility.ToJson(status)) + "&user=" + secrets.HubUser);
			r.SetRequestHeader("Cookie", hubCookie);
			yield return r.SendWebRequest();
			if (r.error != null)
			{
				Logger.Log("Failed to update hub with server status" + r.error, Category.DatabaseAPI);
			}
		}

		private int GetPort()
		{
			int port = (publicInfo.ServerPort != 0) ? publicInfo.ServerPort : 7777;

			if (telepathyTransport != null)
			{
				return Convert.ToInt32(telepathyTransport.port);
			}

			if (ignoranceTransport != null)
			{
				return Convert.ToInt32(ignoranceTransport.port);
			}

			// if (boosterTransport!= null)
			// {
			// 	return Convert.ToInt32(boosterTransport.boosterPort);
			// }

			return port;
		}
	}

	[Serializable]
	public class HubLoginReq
	{
		public string username;
		public string password;
	}

	[Serializable]
	public class ApiResponse
	{
		public int errorCode = 0; //0 = all good, read the message variable now, otherwise read errorMsg
		public string errorMsg;
		public string message;
	}

	//Used to identify the build and fork of this client/server
	[Serializable]
	public class BuildInfo
	{
		//This is used in the HUB to determine if the player has the right
		//build for your server. Remember 01 is not a valid integer. Make sure it starts with at least 1
		public int BuildNumber;
		//I.E. Unitystation, ColonialMarines, BeeStation
		public string ForkName;
	}
}
