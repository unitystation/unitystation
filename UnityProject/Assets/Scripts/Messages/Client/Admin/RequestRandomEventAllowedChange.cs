using Mirror;
using DiscordWebhook;
using InGameEvents;


namespace Messages.Client.Admin
{
	public class RequestRandomEventAllowedChange : ClientMessage<RequestRandomEventAllowedChange.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool RandomEventsAllowed;
		}

		public override void Process(NetMessage netMsg)
		{
			if (IsFromAdmin() == false) return;

			if (InGameEventsManager.Instance.RandomEventsAllowed == netMsg.RandomEventsAllowed) return;

			InGameEventsManager.Instance.RandomEventsAllowed = netMsg.RandomEventsAllowed;

			var state = netMsg.RandomEventsAllowed ? "ON" : "OFF";
			var msg = $"Admin: {SentByPlayer.Username}, turned random events {state}";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
		}

		public static NetMessage Send(bool randomEventsAllowed = true)
		{
			NetMessage msg = new NetMessage
			{
				RandomEventsAllowed = randomEventsAllowed
			};

			Send(msg);
			return msg;
		}
	}
}
 