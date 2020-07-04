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

        public GameObject ServerInfoUILobbyObject;

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
	        if(string.IsNullOrEmpty(newDesc)) return;
	        ServerInfoUILobbyObject.SetActive(true);
        }
    }

	public class ServerInfoLobbyMessageServer : ServerMessage
	{
		public string ServerName;

		public string ServerDesc;

		public override void Process()
		{
			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUILobby>().ClientSetValues(ServerName, ServerDesc);
		}

		public static ServerInfoLobbyMessageServer Send(NetworkConnection clientConn,string serverName, string serverDesc)
		{
			ServerInfoLobbyMessageServer msg = new ServerInfoLobbyMessageServer
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
			ServerInfoLobbyMessageServer.Send(SentByPlayer.Connection, ServerData.ServerConfig.ServerName, ServerInfoUILobby.serverDesc);
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