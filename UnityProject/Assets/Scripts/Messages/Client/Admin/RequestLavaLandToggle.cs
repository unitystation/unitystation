using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiscordWebhook;
using InGameEvents;
using Messages.Client;
using Mirror;

public class RequestLavaLandToggle : ClientMessage
{
	public class RequestLavaLandToggleNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public bool LavaLandAllowed = true;
	}

	public override void Process<T>(T netMsg)
	{
		var newMsg = netMsg as RequestLavaLandToggleNetMessage;
		if(newMsg == null) return;

		var admin = PlayerList.Instance.GetAdmin(newMsg.Userid, newMsg.AdminToken);
		if (admin == null) return;

		if(SubSceneManager.AdminAllowLavaland == newMsg.LavaLandAllowed) return;

		SubSceneManager.AdminAllowLavaland = newMsg.LavaLandAllowed;

		var state = newMsg.LavaLandAllowed ? "ON" : "OFF";
		var msg = $"Admin: {PlayerList.Instance.GetByUserID(newMsg.Userid).Username}, Turned Lava Land spawning {state}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	public static RequestLavaLandToggleNetMessage Send(string userId, string adminToken, bool lavaLandAllowed)
	{
		RequestLavaLandToggleNetMessage msg = new RequestLavaLandToggleNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			LavaLandAllowed = lavaLandAllowed
		};
		new RequestLavaLandToggle().Send(msg);
		return msg;
	}
}
