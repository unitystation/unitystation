using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using IgnoranceTransport;
using Logs;
using Newtonsoft.Json;
using RCON;
using SecureStuff;
using US13.Managers;
using US13.Managers.SubSceneManager;
using US13.UI.Systems.ServerInfoPanel.Models;
using Util;

namespace US13.Core.Database
{
	/// <summary>
	///     If activated on a server then it will update the
	///     unitystation rest api with the status of this server.
	///     To gain access to the unitystation hub for your server
	///     speak to unitystation staff on discord.
	/// </summary>
	public partial class ServerData
	{
		private BuildInfo buildInfo;
		private ServerConfig config;
		private Ignorance ignoranceTransport;
		private ServerMotdData motdData;
		private string rulesData;

		/// <summary>
		///     The server config that holds the values
		///     for your RCON and Unitystation HUB API connections
		/// </summary>
		public static ServerConfig ServerConfig => Instance.config;

		public static ServerMotdData MotdData => Instance.motdData;
		public static string RulesData => Instance.rulesData;

		// we need to do this in a function because initialisation is hard
		private static string GetBackendUrl()
		{
			return new UriBuilder(Uri.UriSchemeHttps, GameManager.Instance.AccountAPIHost)
			{
				Path = "baby-serverlist/status/"
			}.Uri.ToString();
		}

		private void AttemptConfigLoad()
		{
			try
			{
				buildInfo = JsonConvert.DeserializeObject<BuildInfo>(AccessFile.Load("buildinfo.json"));
			}
			catch (Exception e)
			{
				Loggy.Info(
					$"[ServerData.ServerStatus/AttemptConfigLoad()] - Something went wrong while trying to load buildinfo \n {e}",
					Category.DatabaseAPI);
			}

			if (AccessFile.Exists("config.json") == false)
			{
				Loggy.Info("No config found for Rcon and Server Hub connections", Category.DatabaseAPI);
				return;
			}

			ServerConfig finalConfig = new();
			try
			{
				var constants = JsonConvert.DeserializeObject<ServerConstants>(AccessFile.Load("config.json"));
				finalConfig.WithConstants(constants);
			}
			catch (Exception e)
			{
				Loggy.Error(
					$"[ServerData.ServerStatus/AttemptConfigLoad()] - Something went wrong while trying to load config.json. \n {e}", Category.DatabaseAPI);
			}

			try
			{
				var secrets = JsonConvert.DeserializeObject<ServerSecrets>(AccessFile.Load("secrets.json"));
				finalConfig.WithSecrets(secrets);
			}
			catch (Exception e)
			{
				Loggy.Error(
					$"[ServerData.ServerStatus/AttemptConfigLoad()] - Something went wrong while trying to load secrets.json. \n {e}", Category.DatabaseAPI);
			}

			ignoranceTransport = FindFirstObjectByType<Ignorance>();
			config = finalConfig;
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


		private static async UniTask<string> GetPublicIp()
		{
			HttpResponseMessage response = await SafeHttpRequest.GetAsync("http://ipinfo.io/ip");
			if (response is { IsSuccessStatusCode: true })
			{
				var ipResponse = await response.Content.ReadAsStringAsync();
				return Regex.Replace(ipResponse, @"\t|\n|\r", "");
			}

			Loggy.Error("Unable to get public IP address.");
			return "Unknown";
		}

		private int GetPort()
		{
			var port = config.ServerPort != 0 ? config.ServerPort : 7777;

			return ignoranceTransport != null
				? Convert.ToInt32(ignoranceTransport.port)
				: port;
		}

		private async UniTask SendServerStatus()
		{
			ServerStatus status = new()
			{
				ServerToken = config.ServerToken,
				ServerName = config.ServerName,
				ForkName = buildInfo.ForkName,
				BuildVersion = buildInfo.BuildNumber,
				GoodFileVersion = buildInfo.GoodFileVersion,
				CurrentMap = SubSceneManager.Instance.OrNull() is null
					? "Loading"
					:  Path.GetFileName(SubSceneManager.ServerChosenMainStation).Replace(".json", ""),
				Passworded = !string.IsNullOrEmpty(config.ConnectionPassword),
				RoundTime = GameManager.Instance.RoundTimeInMinutes.ToString(),
				PlayerCountMax = GameManager.Instance.PlayerLimit,
				GameMode = GameManager.Instance.GetGameModeName(),
				IngameTime = GameManager.Instance.roundTimer.text,
				PlayerCount = PlayerList.Instance.OrNull() is null
					? 0
					: PlayerList.Instance.ConnectionCount,
				ServerIP = !string.IsNullOrEmpty(config.PublicAddress)
					? config.PublicAddress
					: config.BindAddress,
				ServerPort = GetPort(),
				WinDownload = config.WinDownload,
				OSXDownload = config.OSXDownload,
				LinuxDownload = config.LinuxDownload,
				fps = (int)FPSMonitor.Instance.Current
			};

			await UniTask.SwitchToThreadPool();

			if (string.IsNullOrEmpty(status.ServerIP))
			{
				status.ServerIP = await GetPublicIp();
			}

			HttpResponseMessage response = null;
			Exception requestException = null;

			try
			{
				StringContent request = new(JsonConvert.SerializeObject(status), Encoding.UTF8, "application/json");
				response = await SafeHttpRequest.PostAsync(GetBackendUrl(), request);
			}
			catch (Exception ex)
			{
				requestException = ex;
			}

			await UniTask.SwitchToMainThread();

			if (requestException != null)
			{
				Loggy.Error().Format(
					"Failed to update server status to server list. Error: {0}",
					Category.DatabaseAPI,
					requestException);
				return;
			}

			if (response is not { IsSuccessStatusCode: true })
			{
				var reason = response?.ReasonPhrase ?? "Unknown error";
				Loggy.Error().Format(
					"Failed to update server status to server list. Error: {0}",
					Category.DatabaseAPI,
					reason);
				return;
			}

			Loggy.Trace("Successfully updated server status to server list.");
		}
	}

	[Serializable]
	public class ApiResponse
	{
		public int errorCode; //0 = all good, read the message variable now, otherwise read errorMsg
		public string errorMsg;
		public string message;
	}

	[Serializable]
	public class ServerStatus
	{
		public string ServerToken;
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
		public string GoodFileVersion;
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

		// used to publish your server on the server list
		public string ServerToken;

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

		public ServerConfig WithConstants(ServerConstants constants)
		{
			RconPort = constants.RconPort;
			ServerPort = constants.ServerPort;
			BindAddress = constants.BindAddress;
			PublicAddress = constants.PublicAddress;
			ServerName = constants.ServerName;
			WinDownload = constants.WinDownload;
			OSXDownload = constants.OSXDownload;
			LinuxDownload = constants.LinuxDownload;
			DiscordLinkID = constants.DiscordLinkID;
			AddressableCatalogues =  constants.AddressableCatalogues;
			LobbyAddressableCatalogues =  constants.LobbyAddressableCatalogues;
			ConnectionPassword = constants.ConnectionPassword;

			return this;
		}

		public ServerConfig WithSecrets(ServerSecrets secrets)
		{
			RconPass = secrets.RconPass;
			certKey =  secrets.certKey;
			ServerToken = secrets.ServerToken;
			DiscordWebhookOOCURL  = secrets.DiscordWebhookOOCURL;
			DiscordWebhookOOCMentionsID  = secrets.DiscordWebhookOOCMentionsID;
			DiscordWebhookAdminURL  = secrets.DiscordWebhookAdminURL;
			DiscordWebhookAnnouncementURL  = secrets.DiscordWebhookAnnouncementURL;
			DiscordWebhookEnableBanKickAnnouncement  = secrets.DiscordWebhookEnableBanKickAnnouncement;
			DiscordWebhookAllChatURL   = secrets.DiscordWebhookAllChatURL;
			DiscordWebhookSendOOCToAllChat  = secrets.DiscordWebhookSendOOCToAllChat;
			DiscordWebhookAdminLogURL  = secrets.DiscordWebhookAdminLogURL;
			DiscordWebhookErrorLogURL  = secrets.DiscordWebhookErrorLogURL;
			return this;
		}
	}

	[Serializable]
	public class ServerSecrets
	{
		public string RconPass;
		//CertKey needed in the future for SSL Rcon
		public string certKey;

		// used to publish your server on the server list
		public string ServerToken;

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
	}

	public class ServerConstants
	{
		public int RconPort;
		public int ServerPort;
		public string BindAddress;

		public string PublicAddress;

		public string ServerName;

		//Location on the internet where clients can be downloaded from:
		public string WinDownload;
		public string OSXDownload;
		public string LinuxDownload;

		//End of a discord invite used for serverinfo page
		public string DiscordLinkID;

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

		//What good file version does this build use
		public string GoodFileVersion;
	}
}