using Mirror;
using Newtonsoft.Json;
using US13.Messages.Client.Admin;
using US13.UI.Systems;
using US13.UI.Systems.AdminTools;

namespace US13.Messages.Server.AdminTools
{
	public class AdminChatUpdateMessage : ServerMessage<AdminChatUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ClientUpdateChatLog(msg.JsonData);
		}

		public static NetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);
			NetMessage  msg =
				new NetMessage  {JsonData = JsonConvert.SerializeObject(update) };

			SendToAdmins(msg, TAG.ADMIN_CHAT);
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
