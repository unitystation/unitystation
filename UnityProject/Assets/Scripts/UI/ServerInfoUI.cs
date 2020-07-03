using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;

namespace ServerInfo
{
	public class ServerInfoUI : MonoBehaviour
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

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

	        Debug.LogError("name1 "+ nameText);
	        Debug.LogError("desc1 "+ descText);

	        ServerName.text = nameText;
	        ServerDesc.text = descText;
	        serverDesc = descText;
        }

        public void ClientSetValues(string newName, string newDesc)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
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