using AdminTools;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class AdminLogUpdateMessage : ServerMessage<AdminLogUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminLogWindow.ClientUpdateChatLog(msg.JsonData);
		}

		public static NetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);
			NetMessage  msg =
				new NetMessage  {JsonData = JsonConvert.SerializeObject(update) };

			SendToAdmins(msg);
			return msg;
		}

		public static NetMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update)
		{
			NetMessage msg =
				new NetMessage
				{
					JsonData = JsonConvert.SerializeObject(update),
				};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
