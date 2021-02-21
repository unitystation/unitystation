using System.Collections;
using System.Collections.Generic;
using InGameEvents;
using UnityEngine;
using DiscordWebhook;
using Messages.Client;
using Mirror;

public class RequestRandomEventAllowedChange : ClientMessage
{
	public struct RequestRandomEventAllowedChangeNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public bool RandomEventsAllowed;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestRandomEventAllowedChangeNetMessage IgnoreMe;

	public override void Process<T>(T netMsg)
	{
		var newMsgNull = netMsg as RequestRandomEventAllowedChangeNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		var admin = PlayerList.Instance.GetAdmin(newMsg.Userid, newMsg.AdminToken);
		if (admin == null) return;

		if(InGameEventsManager.Instance.RandomEventsAllowed == newMsg.RandomEventsAllowed) return;

		InGameEventsManager.Instance.RandomEventsAllowed = newMsg.RandomEventsAllowed;

		var state = newMsg.RandomEventsAllowed ? "ON" : "OFF";
		var msg = $"Admin: {PlayerList.Instance.GetByUserID(newMsg.Userid).Username}, Turned random events {state}";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	public static RequestRandomEventAllowedChangeNetMessage Send(string userId, string adminToken, bool randomEventsAllowed = true)
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