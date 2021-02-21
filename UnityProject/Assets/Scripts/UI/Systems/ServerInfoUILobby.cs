using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;
using System;
using Initialisation;
using Messages.Client;
using UnityEngine.UI;

namespace ServerInfo
{
	public class ServerInfoUILobby : MonoBehaviour, IInitialise
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

        public GameObject ServerInfoUILobbyObject;

        public GameObject DiscordButton;

        public static string serverDesc;

        public static string serverDiscordID;

        public InitialisationSystems Subsystem => InitialisationSystems.ServerInfoUILobby;

        void IInitialise.Initialise()
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
		public class ServerInfoLobbyMessageServerNetMessage : NetworkMessage
		{
			public string ServerName;
			public string ServerDesc;
			public string ServerDiscordID;
		}

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as ServerInfoLobbyMessageServerNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUILobby>().ClientSetValues(newMsg.ServerName, newMsg.ServerDesc, newMsg.ServerDiscordID);
		}

		public static ServerInfoLobbyMessageServerNetMessage Send(NetworkConnection clientConn,string serverName, string serverDesc, string serverDiscordID)
		{
			ServerInfoLobbyMessageServerNetMessage msg = new ServerInfoLobbyMessageServerNetMessage
			{
				ServerName = serverName,
				ServerDesc = serverDesc,
				ServerDiscordID = serverDiscordID
			};

			new ServerInfoLobbyMessageServer().SendTo(clientConn, msg);
			return msg;
		}
	}

	public class ServerInfoLobbyMessageClient : ClientMessage
	{
		public class ServerInfoLobbyMessageClientNetMessage : NetworkMessage
		{
			public string PlayerId;
		}

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as ServerInfoLobbyMessageClientNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			ServerInfoLobbyMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUILobby.serverDesc, ServerInfoUILobby.serverDiscordID);
		}

		public static ServerInfoLobbyMessageClientNetMessage Send(string playerId)
		{
			ServerInfoLobbyMessageClientNetMessage msg = new ServerInfoLobbyMessageClientNetMessage
			{
				PlayerId = playerId,
			};

			new ServerInfoLobbyMessageClient().Send(msg);
			return msg;
		}
	}
}