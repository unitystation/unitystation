using Mirror;
using DiscordWebhook;


namespace Messages.Client.Admin
{
	public class RequestLavaLandToggle : ClientMessage<RequestLavaLandToggle.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public bool LavaLandAllowed;
		}

		public override void Process(NetMessage netMsg)
		{
			if (IsFromAdmin() == false) return;

			if (SubSceneManager.AdminAllowLavaland == netMsg.LavaLandAllowed) return;

			SubSceneManager.AdminAllowLavaland = netMsg.LavaLandAllowed;

			var state = netMsg.LavaLandAllowed ? "ON" : "OFF";
			var msg = $"Admin: {SentByPlayer.Username}, turned Lava Land spawning {state}";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
		}

		public static NetMessage Send(bool lavaLandAllowed = true)
		{
			NetMessage msg = new NetMessage
			{
				LavaLandAllowed = lavaLandAllowed
			};

			Send(msg);
			return msg;
		}
	}
}
