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
	public class ServerInfoUILobby : MonoBehaviour
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

        public static string serverDesc;

        public Scrollbar scrollbarOnServerInfo;

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
	        scrollbarOnServerInfo.value = 1;
        }

        public void ClientSetValues(string newName, string newDesc)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
	        scrollbarOnServerInfo.value = 1;
        }
    }

	public class ServerInfoLobbyMessageServer : ServerMessage
	{
		public string ServerName;

		public string ServerDesc;

		public override void Process()
		{
			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUI>().ClientSetValues(ServerName, ServerDesc);
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

	public class ServerInfoLobbyMessageClient : ClientMessage
	{
		public string PlayerId;

		public override void Process()
		{
			ServerInfoMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUILobby.serverDesc);
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