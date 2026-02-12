using Mirror;
using Newtonsoft.Json;
using US13.Messages.Client.Admin;
using US13.UI.Systems;
using US13.UI.Systems.AdminTools;

namespace US13.Messages.Server.AdminTools
{
	public class AdminPlayerChatUpdateMessage : ServerMessage<AdminPlayerChatUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public string PlayerId;
			public int RoundID;
			public bool ForceShow;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminPlayerChat.ClientUpdateChatLog(msg.JsonData, msg.PlayerId, msg.RoundID, msg.ForceShow);
		}

		public static NetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage, string playerId, int RoundID, bool ForceShow)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);
			NetMessage  msg =
				new NetMessage
				{
					JsonData = JsonConvert.SerializeObject(update), PlayerId = playerId,
					RoundID = RoundID,
					ForceShow = ForceShow
				};

			SendToAdmins(msg, TAG.PLAYER_AHELP);
			return msg;
		}

		public static NetMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update, string playerId, int RoundID, bool ForceShow)
		{
			NetMessage msg =
				new NetMessage
				{
					JsonData = JsonConvert.SerializeObject(update),
					PlayerId = playerId,
					RoundID = RoundID,
					ForceShow = ForceShow
				};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
