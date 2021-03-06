using DiscordWebhook;
using InGameEvents;
using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestRandomEventAllowedChange : ClientMessage<RequestRandomEventAllowedChange.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public bool RandomEventsAllowed;
		}

		public override void Process(NetMessage netMsg)
		{
			var admin = PlayerList.Instance.GetAdmin(netMsg.Userid, netMsg.AdminToken);
			if (admin == null) return;

			if(InGameEventsManager.Instance.RandomEventsAllowed == netMsg.RandomEventsAllowed) return;

			InGameEventsManager.Instance.RandomEventsAllowed = netMsg.RandomEventsAllowed;

			var state = netMsg.RandomEventsAllowed ? "ON" : "OFF";
			var msg = $"Admin: {PlayerList.Instance.GetByUserID(netMsg.Userid).Username}, Turned random events {state}";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
		}

		public static NetMessage Send(string userId, string adminToken, bool randomEventsAllowed = true)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				RandomEventsAllowed = randomEventsAllowed
			};

			Send(msg);
			return msg;
		}
	}
}