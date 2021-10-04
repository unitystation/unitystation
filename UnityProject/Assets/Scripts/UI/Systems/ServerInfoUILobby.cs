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
using Messages.Server;
using UI;

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
	        if (string.IsNullOrEmpty(ServerData.ServerConfig.DiscordLinkID)) return;

	        serverDiscordID = ServerData.ServerConfig.DiscordLinkID;
        }

        public void ClientSetValues(string newName, string newDesc, string newDiscordID)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
	        serverDiscordID = newDiscordID;

	        if (string.IsNullOrEmpty(newDesc)) return;
	        ServerInfoUILobbyObject.SetActive(true);
	        if (string.IsNullOrEmpty(newDiscordID)) return;
	        DiscordButton.SetActive(true);
	        DiscordButton.GetComponent<OpenURL>().url = "https://discord.gg/" + newDiscordID;
        }

        [Serializable]
        public class ServerInfoLinks
        {
	        public string DiscordLinkID;
        }
    }

	public class ServerInfoLobbyMessageServer : ServerMessage<ServerInfoLobbyMessageServer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ServerName;
			public string ServerDesc;
			public string ServerDiscordID;
		}

		public override void Process(NetMessage msg)
		{
			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUILobby>().ClientSetValues(
					msg.ServerName, msg.ServerDesc, msg.ServerDiscordID);
		}

		public static NetMessage Send(NetworkConnection clientConn,string serverName, string serverDesc, string serverDiscordID)
		{
			NetMessage msg = new NetMessage
			{
				ServerName = serverName,
				ServerDesc = serverDesc,
				ServerDiscordID = serverDiscordID
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}

	public class ServerInfoLobbyMessageClient : ClientMessage<ServerInfoLobbyMessageClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			ServerInfoLobbyMessageServer.Send(
					SentByPlayer.Connection, ServerData.ServerConfig.ServerName,
					ServerInfoUILobby.serverDesc, ServerInfoUILobby.serverDiscordID);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
