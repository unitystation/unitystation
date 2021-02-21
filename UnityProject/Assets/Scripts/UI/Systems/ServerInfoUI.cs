using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;
using Initialisation;
using Messages.Client;
using UnityEngine.UI;

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
	        if(string.IsNullOrEmpty(ServerInfoUILobby.serverDiscordID)) return;
	        DiscordButton.SetActive(true);
	        DiscordButton.GetComponent<OpenURL>().url = "https://discord.gg/" + ServerInfoUILobby.serverDiscordID;
        }
    }

	public class ServerInfoMessageServer : ServerMessage
	{
		public struct ServerInfoMessageServerNetMessage : NetworkMessage
		{
			public string ServerName;
			public string ServerDesc;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public ServerInfoMessageServerNetMessage IgnoreMe;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as ServerInfoMessageServerNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			GUI_IngameMenu.Instance.GetComponent<ServerInfoUI>().ClientSetValues(newMsg.ServerName, newMsg.ServerDesc);
		}

		public static ServerInfoMessageServerNetMessage Send(NetworkConnection clientConn,string serverName, string serverDesc)
		{
			ServerInfoMessageServerNetMessage msg = new ServerInfoMessageServerNetMessage
			{
				ServerName = serverName,
				ServerDesc = serverDesc
			};

			new ServerInfoMessageServer().SendTo(clientConn, msg);
			return msg;
		}
	}

	public class ServerInfoMessageClient : ClientMessage
	{
		public struct ServerInfoMessageClientNetMessage : NetworkMessage
		{
			public string PlayerId;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public ServerInfoMessageClientNetMessage IgnoreMe;

		public override void Process<T>(T msg)
		{
			var newMsgNull = msg as ServerInfoMessageClientNetMessage?;
			if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

			ServerInfoMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUI.serverDesc);
		}

		public static ServerInfoMessageClientNetMessage Send(string playerId)
		{
			ServerInfoMessageClientNetMessage msg = new ServerInfoMessageClientNetMessage
			{
				PlayerId = playerId,
			};

			new ServerInfoMessageClient().Send(msg);
			return msg;
		}
	}
}