using System.Collections.Generic;
using System.Linq;
using AdminTools;
using Mirror;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminPlayerListRefreshMessage : ServerMessage<AdminPlayerListRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Recipient);
			var listData = JsonUtility.FromJson<AdminPlayersList>(msg.JsonData);

			foreach (var v in UIManager.Instance.adminChatWindows.playerListViews)
			{
				if (v.gameObject.activeInHierarchy)
				{
					v.ReceiveUpdatedPlayerList(listData);
				}
			}
		}

		public static NetMessage Send(GameObject recipient, string adminID)
		{
			AdminPlayersList playerList = new AdminPlayersList
			{
				//Player list info:
				players = AdminToolRefreshMessage.GetAllPlayerStates(adminID, true)
			};

			var data = JsonUtility.ToJson(playerList);

			NetMessage  msg =
				new NetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
