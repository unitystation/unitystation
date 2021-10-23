using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using TMPro;
using DatabaseAPI;
using Initialisation;
using Messages.Client;
using Messages.Server;
using UI;


namespace ServerInfo
{
	public class ServerInfoUI : MonoBehaviour, IInitialise
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

        public GameObject DiscordButton;

        public static string serverDesc;

        public InitialisationSystems Subsystem => InitialisationSystems.ServerInfoUI;

        void IInitialise.Initialise()
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

        public void ClientSetValues(string newName, string newDesc)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
	        if (string.IsNullOrEmpty(ServerInfoUILobby.serverDiscordID)) return;
	        DiscordButton.SetActive(true);
	        DiscordButton.GetComponent<OpenURL>().url = "https://discord.gg/" + ServerInfoUILobby.serverDiscordID;
        }
    }

	public class ServerInfoMessageServer : ServerMessage<ServerInfoMessageServer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ServerName;
			public string ServerDesc;
		}

		public override void Process(NetMessage msg)
		{
			GUI_IngameMenu.Instance.GetComponent<ServerInfoUI>().ClientSetValues(msg.ServerName, msg.ServerDesc);
		}

		public static NetMessage Send(NetworkConnection clientConn,string serverName, string serverDesc)
		{
			NetMessage msg = new NetMessage
			{
				ServerName = serverName,
				ServerDesc = serverDesc
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}

	public class ServerInfoMessageClient : ClientMessage<ServerInfoMessageClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			ServerInfoMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUI.serverDesc);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
