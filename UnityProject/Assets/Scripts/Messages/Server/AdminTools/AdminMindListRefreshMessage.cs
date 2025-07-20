using AdminTools;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;


namespace Messages.Server.AdminTools
{
	public class AdminMindListRefreshMessage : ServerMessage<AdminMindListRefreshMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
		}

		public override void Process(NetMessage msg)
		{

			LoadNetworkObject(msg.Recipient);
			var listData = JsonConvert.DeserializeObject<AdminMindList>(msg.JsonData);

			GUI_AdminTools.Instance.adminMindScrollView.ReceiveUpdatedPlayerList(listData);
		}

		public static NetMessage Send(GameObject recipient, string adminID)
		{
			AdminMindList playerList = new AdminMindList
			{
				//Player list info:
				players = MindManager.Instance.GetMindStates()
			};

			var data = JsonConvert.SerializeObject(playerList);

			NetMessage  msg =
				new NetMessage  {Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = data};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
