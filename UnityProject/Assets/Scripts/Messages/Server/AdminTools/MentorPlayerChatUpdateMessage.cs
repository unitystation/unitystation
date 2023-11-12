using AdminTools;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	public class MentorPlayerChatUpdateMessage : ServerMessage<MentorPlayerChatUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public string PlayerId;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.mentorPlayerChat.ClientUpdateChatLog(msg.JsonData, msg.PlayerId);
		}

		public static NetMessage SendSingleEntryToMentors(AdminChatMessage chatMessage, string playerId)
		{
			AdminChatUpdate update = new AdminChatUpdate();
			update.messages.Add(chatMessage);

			NetMessage  msg = new NetMessage
			{
				JsonData = JsonConvert.SerializeObject(update),
				PlayerId = playerId
			};

			SendToMentors(msg);
			return msg;
		}

		public static NetMessage SendLogUpdateToMentor(NetworkConnection requestee, AdminChatUpdate update, string playerId)
		{
			NetMessage msg = new NetMessage
			{
				JsonData = JsonConvert.SerializeObject(update),
				PlayerId = playerId
			};

			SendTo(requestee, msg);
			return msg;
		}
	}
}
