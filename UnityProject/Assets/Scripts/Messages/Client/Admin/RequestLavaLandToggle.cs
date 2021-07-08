using DiscordWebhook;
using Mirror;

namespace Messages.Client.Admin
{
	public class RequestLavaLandToggle : ClientMessage<RequestLavaLandToggle.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Userid;
			public string AdminToken;
			public bool LavaLandAllowed;
		}

		public override void Process(NetMessage netMsg)
		{
			var admin = PlayerList.Instance.GetAdmin(netMsg.Userid, netMsg.AdminToken);
			if (admin == null) return;

			if(SubSceneManager.AdminAllowLavaland == netMsg.LavaLandAllowed) return;

			SubSceneManager.AdminAllowLavaland = netMsg.LavaLandAllowed;

			var state = netMsg.LavaLandAllowed ? "ON" : "OFF";
			var msg = $"Admin: {PlayerList.Instance.GetByUserID(netMsg.Userid).Username}, Turned Lava Land spawning {state}";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
		}

		public static NetMessage Send(string userId, string adminToken, bool lavaLandAllowed = true)
		{
			NetMessage msg = new NetMessage
			{
				Userid = userId,
				AdminToken = adminToken,
				LavaLandAllowed = lavaLandAllowed
			};
			Send(msg);
			return msg;
		}
	}
}
