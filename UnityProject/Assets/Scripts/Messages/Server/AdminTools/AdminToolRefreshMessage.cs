using System.Collections.Generic;
using System.Linq;
using AdminTools;
using DatabaseAPI;
using InGameEvents;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminToolRefreshMessage : ServerMessage<AdminToolRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public NetMessage IgnoreMe;

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var adminPageData = JsonConvert.DeserializeObject<AdminPageRefreshData>(msg.JsonData);

			var pages = GameObject.FindObjectsOfType<AdminPage>();
			foreach (var g in pages)
			{
				g.GetComponent<AdminPage>().OnPageRefresh(adminPageData);
			}
		}

		public static NetMessage Send(GameObject recipient, string adminID)
		{
			//Gather the data:
			var pageData = new AdminPageRefreshData();

			//Game Mode Information:
			pageData.availableGameModes = GameManager.Instance.GetAvailableGameModeNames();
			pageData.isSecret = GameManager.Instance.SecretGameMode;
			pageData.currentGameMode = GameManager.Instance.GetGameModeName(true);
			pageData.nextGameMode = GameManager.Instance.NextGameMode;

			//Event Manager
			pageData.randomEventsAllowed = InGameEventsManager.Instance.RandomEventsAllowed;

			//Round Manager
			pageData.nextMap = SubSceneManager.AdminForcedMainStation;
			pageData.nextAwaySite = SubSceneManager.AdminForcedAwaySite;
			pageData.allowLavaLand = SubSceneManager.AdminAllowLavaland;
			pageData.alertLevel = GameManager.Instance.CentComm.CurrentAlertLevel.ToString();

			//Centcom
			pageData.blockCall = GameManager.Instance.PrimaryEscapeShuttle.blockCall;
			pageData.blockRecall = GameManager.Instance.PrimaryEscapeShuttle.blockRecall;

			//Player list info:
			pageData.players = GetAllPlayerStates(adminID);

			//Server Setting
			pageData.playerLimit = GameManager.Instance.PlayerLimit;
			pageData.maxFrameRate = Application.targetFrameRate;
			pageData.serverPassword = ServerData.ServerConfig.ConnectionPassword;

			var data = JsonConvert.SerializeObject(pageData);

			NetMessage  msg =
				new NetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}

		public static List<AdminPlayerEntryData> GetAllPlayerStates(string adminID, bool onlineOnly = false)
		{
			var playerList = new List<AdminPlayerEntryData>();
			if (string.IsNullOrEmpty(adminID)) return playerList;

			var toSearchThrough = PlayerList.Instance.AllPlayers.ToList();

			if (onlineOnly == false)
			{
				toSearchThrough.AddRange(PlayerList.Instance.loggedOff);
			}

			foreach (var player in toSearchThrough)
			{
				if (player == null) continue;

				var entry = new AdminPlayerEntryData();
				entry.name = player.Name;
				entry.uid = player.UserId;
				entry.currentJob = player.Job.ToString();
				entry.accountName = player.Username;

				entry.ipAddress = player.ConnectionIP;

				if (player.Script != null && player.Script.playerHealth != null)
				{
					entry.isAlive = player.Script.playerHealth.ConsciousState != ConsciousState.DEAD;
				}

				entry.isAntag = PlayerList.Instance.AntagPlayers.Contains(player);
				entry.isAdmin = PlayerList.Instance.IsAdmin(player.UserId);
				entry.isMentor = PlayerList.Instance.IsMentor(player.UserId);
				entry.isOnline = player.Connection != null;
				entry.isOOCMuted = player.IsOOCMuted;

				playerList.Add(entry);
			}

			return playerList.OrderBy(p => p.name).ThenBy(p => p.isOnline).ToList();
		}
	}
}
