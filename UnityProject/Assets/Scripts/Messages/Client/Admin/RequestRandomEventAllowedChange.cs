using System.Collections;
using System.Collections.Generic;
using InGameEvents;
using UnityEngine;
using DiscordWebhook;

public class RequestRandomEventAllowedChange : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public bool RandomEventsAllowed = true;

	public override void Process()
	{
		var admin = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (admin == null) return;

		if(InGameEventsManager.Instance.RandomEventsAllowed == RandomEventsAllowed) return;

		InGameEventsManager.Instance.RandomEventsAllowed = RandomEventsAllowed;

		var state = RandomEventsAllowed ? "ON" : "OFF";
		var msg = $"Admin: {PlayerList.Instance.GetByUserID(Userid).Username}, Turned random events {state}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	public static RequestRandomEventAllowedChange Send(string userId, string adminToken, bool randomEventsAllowed)
	{
		RequestRandomEventAllowedChange msg = new RequestRandomEventAllowedChange
		{
			Userid = userId,
			AdminToken = adminToken,
			RandomEventsAllowed = randomEventsAllowed
		};
		msg.Send();
		return msg;
	}
}