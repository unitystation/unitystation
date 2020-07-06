using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;
using UnityEngine.UI;

namespace ServerInfo
{
	public class ServerInfoUI : MonoBehaviour
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

        public GameObject DiscordButton;

        public static string serverDesc;

        public void Start()
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
		public string ServerName;

		public string ServerDesc;

		public override void Process()
		{
			GUI_IngameMenu.Instance.GetComponent<ServerInfoUI>().ClientSetValues(ServerName, ServerDesc);
		}

		public static ServerInfoMessageServer Send(NetworkConnection clientConn,string serverName, string serverDesc)
		{
			ServerInfoMessageServer msg = new ServerInfoMessageServer
			{
				ServerName = serverName,
				ServerDesc = serverDesc
			};
			msg.SendTo(clientConn);
			return msg;
		}
	}

	public class ServerInfoMessageClient : ClientMessage
	{
		public string PlayerId;

		public override void Process()
		{
			ServerInfoMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUI.serverDesc);
		}

		public static ServerInfoMessageClient Send(string playerId)
		{
			ServerInfoMessageClient msg = new ServerInfoMessageClient
			{
				PlayerId = playerId,
			};
			msg.Send();
			return msg;
		}
	}
}