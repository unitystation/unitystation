using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;
using System;
using UnityEngine.UI;

namespace ServerInfo
{
	public class ServerInfoUILobby : MonoBehaviour
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

        public GameObject ServerInfoUILobbyObject;

        public GameObject DiscordButton;

        public static string serverDesc;

        public static string serverDiscordID;

        public void Start()
        {
	        LoadNameAndDesc();
	        LoadLinks();
        }

        private void LoadNameAndDesc()
        {
	        var path = Path.Combine(Application.streamingAssetsPath, "config", "serverDesc.txt");

	        var descText = "";

	        if (File.Exists(path))
	        {
		        descText = File.ReadAllText(path);
	        }

	        var nameText = ServerData.ServerConfig.ServerName;

	        ServerName.text = nameText;
	        ServerDesc.text = descText;
	        serverDesc = descText;
        }

        private void LoadLinks()
        {
	        var path = Path.Combine(Application.streamingAssetsPath, "config", "serverDescLinks.json");

	        if (!File.Exists(path)) return;

	        var linkList = JsonUtility.FromJson<ServerInfoLinks>(File.ReadAllText(path));

	        serverDiscordID = linkList.DiscordLinkID;
        }

        public void ClientSetValues(string newName, string newDesc, string newDiscordID)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
	        serverDiscordID = newDiscordID;

	        if(string.IsNullOrEmpty(newDesc)) return;
	        ServerInfoUILobbyObject.SetActive(true);
	        if(string.IsNullOrEmpty(newDiscordID)) return;
	        DiscordButton.SetActive(true);
	        DiscordButton.GetComponent<OpenURL>().url = "https://discord.gg/" + newDiscordID;
        }

        [Serializable]
        public class ServerInfoLinks
        {
	        public string DiscordLinkID;
        }
    }

	public class ServerInfoLobbyMessageServer : ServerMessage
	{
		public string ServerName;

		public string ServerDesc;

		public string ServerDiscordID;

		public override void Process()
		{
			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUILobby>().ClientSetValues(ServerName, ServerDesc, ServerDiscordID);
		}

		public static ServerInfoLobbyMessageServer Send(NetworkConnection clientConn,string serverName, string serverDesc, string serverDiscordID)
		{
			ServerInfoLobbyMessageServer msg = new ServerInfoLobbyMessageServer
			{
				ServerName = serverName,
				ServerDesc = serverDesc,
				ServerDiscordID = serverDiscordID
			};
			msg.SendTo(clientConn);
			return msg;
		}
	}

	public class ServerInfoLobbyMessageClient : ClientMessage
	{
		public string PlayerId;

		public override void Process()
		{
			ServerInfoLobbyMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUILobby.serverDesc, ServerInfoUILobby.serverDiscordID);
		}

		public static ServerInfoLobbyMessageClient Send(string playerId)
		{
			ServerInfoLobbyMessageClient msg = new ServerInfoLobbyMessageClient
			{
				PlayerId = playerId,
			};
			msg.Send();
			return msg;
		}
	}
}