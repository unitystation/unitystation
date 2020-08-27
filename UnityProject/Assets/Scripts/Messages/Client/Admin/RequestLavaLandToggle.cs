using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DiscordWebhook;
using InGameEvents;

public class RequestLavaLandToggle : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public bool LavaLandAllowed = true;

	public override void Process()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin == null) return;

		if(SubSceneManager.AdminAllowLavaland == LavaLandAllowed) return;

		SubSceneManager.AdminAllowLavaland = LavaLandAllowed;

		var state = LavaLandAllowed ? "ON" : "OFF";
		var msg = $"Admin: {PlayerList.Instance.GetByUserID(Userid).Username}, Turned Lava Land spawning {state}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	public static RequestLavaLandToggle Send(string userId, string adminToken, bool lavaLandAllowed)
	{
		RequestLavaLandToggle msg = new RequestLavaLandToggle
		{
			Userid = userId,
			AdminToken = adminToken,
			LavaLandAllowed = lavaLandAllowed
		};
		msg.Send();
		return msg;
	}
}
