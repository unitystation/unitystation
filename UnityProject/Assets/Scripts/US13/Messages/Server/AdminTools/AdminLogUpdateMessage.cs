using Mirror;
using Newtonsoft.Json;
using US13.Core.Admin.Logs;
using US13.Messages.Client.Admin;
using US13.UI.Systems.AdminTools;

namespace US13.Messages.Server.AdminTools
{
	public class AdminLogUpdateMessage : ServerMessage<AdminLogUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
		}

		public override void Process(NetMessage msg)
		{
			AdminLogsManager.AddNewLog(null, msg.JsonData, LogCategory.Admin);
		}

		public static NetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);
			NetMessage  msg =
				new NetMessage  {JsonData = JsonConvert.SerializeObject(update) };

			SendToAdmins(msg, TAG.ADMIN_LOGS);
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
