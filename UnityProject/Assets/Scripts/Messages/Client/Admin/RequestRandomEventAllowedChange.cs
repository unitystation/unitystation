using System.Collections;
using System.Collections.Generic;
using InGameEvents;
using UnityEngine;
using DiscordWebhook;
using Messages.Client;

public class RequestRandomEventAllowedChange : ClientMessage
{
	public class RequestRandomEventAllowedChangeNetMessage : ActualMessage
	{
		public string Userid;
		public string AdminToken;
		public bool RandomEventsAllowed = true;
	}

	public override void Process(ActualMessage netMsg)
	{
		var newMsg = netMsg as RequestRandomEventAllowedChangeNetMessage;
		if(newMsg == null) return;

		var admin = PlayerList.Instance.GetAdmin(newMsg.Userid, newMsg.AdminToken);
		if (admin == null) return;

		if(InGameEventsManager.Instance.RandomEventsAllowed == newMsg.RandomEventsAllowed) return;

		InGameEventsManager.Instance.RandomEventsAllowed = newMsg.RandomEventsAllowed;

		var state = newMsg.RandomEventsAllowed ? "ON" : "OFF";
		var msg = $"Admin: {PlayerList.Instance.GetByUserID(newMsg.Userid).Username}, Turned random events {state}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	public static RequestRandomEventAllowedChangeNetMessage Send(string userId, string adminToken, bool randomEventsAllowed)
	{
		RequestRandomEventAllowedChangeNetMessage msg = new RequestRandomEventAllowedChangeNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			RandomEventsAllowed = randomEventsAllowed
		};
		new RequestRandomEventAllowedChange().Send(msg);
		return msg;
	}
}