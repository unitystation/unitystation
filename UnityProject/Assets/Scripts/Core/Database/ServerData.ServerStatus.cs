using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using UnityEngine;
using Mirror;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SecureStuff;
using IgnoranceTransport;
using Logs;
using Managers;
using Newtonsoft.Json;
using UI.Systems.ServerInfoPanel.Models;

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
		private ServerConfig config;
		private ServerMotdData motdData;
		private string rulesData;
		/// <summary>
		/// The server config that holds the values
		/// for your RCON and Unitystation HUB API connections
		/// </summary>
		public static ServerConfig ServerConfig => Instance.config;

		public static ServerMotdData MotdData => Instance.motdData;
		public static string RulesData => Instance.rulesData;

		private BuildInfo buildInfo;

		private string hubCookie;
		private const string hubRoot = "https://api.unitystation.org";
		private const string hubLogin = hubRoot + "/login?data=";
		private const string hubUpdate = hubRoot + "/statusupdate?data=";
		private float updateWait = 0f;
		private string publicIP;
		private Ignorance ignoranceTransport;
		//private BoosterTransport boosterTransport = null;

		//Data.Write( byteArray, Path + "/" + FileName);

		private void AttemptConfigLoad()
		{
			try
			{
				buildInfo = JsonConvert.DeserializeObject<BuildInfo>(AccessFile.Load("buildinfo.json"));
			}
			catch (Exception e)
			{
				Loggy.Log($"[ServerData.ServerStatus/AttemptConfigLoad()] - Something went wrong while trying to load buildinfo \n {e}",
					Category.DatabaseAPI);
			}

			if (AccessFile.Exists("config.json") == false)
			{
				Loggy.Log("No config found for Rcon and Server Hub connections", Category.DatabaseAPI);
				return;
			}
			var configData = new ServerConfig();
			try
			{
				configData = JsonConvert.DeserializeObject<ServerConfig>(AccessFile.Load("config.json"));
			}
			catch (Exception e)
			{
				Loggy.LogError($"[ServerData.ServerStatus/AttemptConfigLoad()] - Something went wrong while trying to load config.json. \n {e}");
			}
			ignoranceTransport = FindObjectOfType<Ignorance>();
			config = configData;
			_ = Instance.SendServerStatus();
		}

		private void LoadMotd()
		{
			var content = AccessFile.Exists("serverDesc.txt") ? AccessFile.Load("serverDesc.txt") : null;
			motdData = new ServerMotdData
			{
				ServerName = config.ServerName,
				ServerDescription = content,
				DiscordLink = config.DiscordLinkID
			};
		}

		private void AttemptRulesLoad()
		{
			rulesData = AccessFile.Exists("serverRules.txt") ? AccessFile.Load("serverRules.txt") : null;
		}


		void MonitorServerStatus()
		{
			updateWait += Time.deltaTime;
			//Update the hub every 10 seconds
			if (updateWait >= 10f)
			{
				updateWait = 0f;
				_=Instance.SendServerStatus();
			}
		}

		private async Task SendServerStatus()
		{

			var status = new ServerStatus();
			var requestData = "";
			try
			{
				if (string.IsNullOrEmpty(config.HubUser) || string.IsNullOrEmpty(config.HubPass))
				{
					Loggy.LogWarning("Invalid Hub creds found, aborting HUB connection");
					return;
				}
				var loginRequest = new HubLoginReq
				{
					username = config.HubUser,
					password = config.HubPass
				};

				status.ServerName = config.ServerName;
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

				status.Passworded = string.IsNullOrEmpty(config.ConnectionPassword) == false;
				status.RoundTime = GameManager.Instance.RoundTimeInMinutes.ToString();
				status.PlayerCountMax = GameManager.Instance.PlayerLimit;


				status.GameMode = GameManager.Instance.GetGameModeName();
				status.IngameTime = GameManager.Instance.roundTimer.text;
				if (PlayerList.Instance != null)
				{
					status.PlayerCount = PlayerList.Instance.ConnectionCount;
				}


				status.ServerIP = publicIP;
				status.ServerPort = GetPort();
				status.WinDownload = config.WinDownload;
				status.OSXDownload = config.OSXDownload;
				status.LinuxDownload = config.LinuxDownload;


				status.fps = (int)FPSMonitor.Instance.Current;
				requestData = JsonConvert.SerializeObject(loginRequest);

			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
				return;
			}


	        try
	        {
	            string escapedData = Uri.EscapeDataString(requestData);
	            HttpResponseMessage response = await  SafeHttpRequest.GetAsync(hubLogin + escapedData);

	            if (response.IsSuccessStatusCode)
	            {
	                string responseBody = await response.Content.ReadAsStringAsync();
	                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseBody);

	                if (apiResponse.errorCode == 0)
	                {

	                    string cookieHeader = response.Headers.GetValues("set-cookie")?.FirstOrDefault();
	                    if (!string.IsNullOrEmpty(cookieHeader))
	                    {
	                        string[] cookieParts = cookieHeader.Split(';');
		                    hubCookie = cookieParts[0];
	                    }

	                    if (!string.IsNullOrEmpty(config.PublicAddress))
	                    {
	                        publicIP = config.PublicAddress;
	                    }
	                    else if (!string.IsNullOrEmpty(config.BindAddress))
	                    {
	                        publicIP = config.BindAddress;
	                    }
	                    else
	                    {
	                        response = await SafeHttpRequest.GetAsync("http://ipinfo.io/ip");
	                        string ipResponse = await response.Content.ReadAsStringAsync();
	                        publicIP = Regex.Replace(ipResponse, @"\t|\n|\r", "");
	                    }
	                }
	                else if (apiResponse.errorCode == 901)
	                {
	                    Loggy.LogError("Hub API returned unauthorized credentials, aborting HUB connection");
	                }
	                else
	                {
	                    Loggy.LogError("Hub API returned error code " + apiResponse.errorCode + ", aborting HUB connection\n" + apiResponse.errorMsg);
	                }
	            }
	            else
	            {
	                Loggy.LogError("Hub API returned error, aborting HUB connection");
	            }
	        }
	        catch (Exception ex)
	        {
	            Loggy.LogError("Error: " + ex.Message);
	        }

			try
			{
				string url = hubUpdate + Uri.EscapeDataString( JsonConvert.SerializeObject(status)) + "&user=" + config.HubUser;

				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
				request.Headers.Add("Cookie", hubCookie);

				HttpResponseMessage response = await SafeHttpRequest.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					Loggy.LogError("Failed to update hub with server status. Error: " + response.ReasonPhrase);
				}
			}
			catch (Exception ex)
			{
				Loggy.LogError("Error: " + ex.Message);
			}
		}


		private int GetPort()
		{
			int port = (config.ServerPort != 0) ? config.ServerPort : 7777;

			if (ignoranceTransport != null)
			{
				return Convert.ToInt32(ignoranceTransport.port);
			}

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

	[Serializable]
	public class ServerStatus
	{
		public bool Passworded;
		public string ServerName;
		public string ForkName;
		public int BuildVersion;
		public string CurrentMap;
		public string GameMode;
		public string IngameTime;
		public string RoundTime;
		public int PlayerCount;
		public int PlayerCountMax;
		public string ServerIP;
		public int ServerPort;
		public string WinDownload;
		public string OSXDownload;
		public string LinuxDownload;
		public int fps;
	}

	//Read from Streaming Assets/config/config.json on the server
	[Serializable]
	public class ServerConfig
	{
		public string RconPass;
		public int RconPort;
		public int ServerPort;
		public string BindAddress;
		public string PublicAddress;
		//CertKey needed in the future for SSL Rcon
		public string certKey;
		public string HubUser;
		public string HubPass;
		public string ServerName;
		//Location on the internet where clients can be downloaded from:
		public string WinDownload;
		public string OSXDownload;
		public string LinuxDownload;

		//End of a discord invite used for serverinfo page
		public string DiscordLinkID;

		//Discord Webhook URL//

		//OOC chat
		public string DiscordWebhookOOCURL;

		//ID that can be pinged in OOC chat
		public string DiscordWebhookOOCMentionsID;

		//Webhook where Ahelps are sent
		public string DiscordWebhookAdminURL;

		//Announcements for round start/end, also public Ban/Kick if enabled
		public string DiscordWebhookAnnouncementURL;
		public bool DiscordWebhookEnableBanKickAnnouncement;

		//Sends all chat messages from each channel, also OOC if enabled
		public string DiscordWebhookAllChatURL;
		public bool DiscordWebhookSendOOCToAllChat;

		//Sends Admin actions to a webhook
		public string DiscordWebhookAdminLogURL;

		//Sends Admin actions to a webhook
		public string DiscordWebhookErrorLogURL;

		//The Catalogue that the client should load when connecting and the catalogues the server loads on its end
		//Catalogues as in addressable catalogues with content
		public List<string> AddressableCatalogues;

		//Built in catalogue content
		//Such as Lobby music
		public List<string> LobbyAddressableCatalogues;

		//The password to join the server if set
		public string ConnectionPassword;
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
