using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DiscordWebhook;
using InGameEvents;

/// <summary>
/// Admin Commands manager, stores admin commands, commands can be run in lobby.
/// </summary>
public class AdminCommandsManager : NetworkBehaviour
{
	private static AdminCommandsManager instance;

	public static AdminCommandsManager Instance
	{
		get
		{
			if (!instance)
			{
				instance = FindObjectOfType<AdminCommandsManager>();
			}

			return instance;
		}
	}

	/// <summary>
	/// If adding a new set of commands not on AdminTools UI then this needs to be called when its open first time.
	/// </summary>
	/// <param name="adminId"></param>
	/// <param name="adminToken"></param>
	public void CheckAuthority(string adminId, string adminToken)
	{
		if (!netIdentity.hasAuthority)
		{
			ServerAuthorityMessageClient.Send(adminId, adminToken);
		}
	}

	#region GamemodePage

	[Command]
	public void CmdToggleOOCMute(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		string msg;

		if (Chat.OOCMute)
		{
			Chat.OOCMute = false;
			msg = "OOC has been unmuted";
		}
		else
		{
			Chat.OOCMute = true;
			msg = "OOC has been muted";
		}

		Chat.AddGameWideSystemMsgToChat($"<color=blue>{msg}</color>");
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, msg, "");
	}

	#endregion

	#region EventsPage

	[Command]
	public void CmdTriggerGameEvent(string adminId, string adminToken, int eventIndex, bool isFake, bool announceEvent,
		InGameEventType eventType)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		InGameEventsManager.Instance.TriggerSpecificEvent(eventIndex, eventType, isFake,
			PlayerList.Instance.GetByUserID(adminId).Username, announceEvent);
	}

	#endregion

	#region RoundPage

	[Command]
	public void CmdStartRound(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (GameManager.Instance.CurrentRoundState == RoundState.PreRound && GameManager.Instance.waitForStart)
		{
			GameManager.Instance.StartRound();

			var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Force STARTED the round.";

			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
			DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg,
				"");
		}
	}

	[Command]
	public void CmdEndRound(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		VideoPlayerMessage.Send(VideoType.RestartRound);
		GameManager.Instance.EndRound();

		var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: Force ENDED the round.";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	[Command]
	public void CmdCallShuttle(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (GameManager.Instance.PrimaryEscapeShuttle.Status == EscapeShuttleStatus.DockedCentcom)
		{
			GameManager.Instance.PrimaryEscapeShuttle.CallShuttle(out var result);
		}

		var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: CALLED the emergency shuttle.";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	[Command]
	public void CmdRecallShuttle(string adminId, string adminToken)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		GameManager.Instance.PrimaryEscapeShuttle.RecallShuttle(out var result, true);

		var msg = $"{PlayerList.Instance.GetByUserID(adminId).Username}: RECALLED the emergency shuttle.";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");
	}

	[Command]
	public void CmdChangeNextMap(string adminId, string adminToken, string nextMap)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (SubSceneManager.AdminForcedMainStation == nextMap) return;

		var msg =
			$"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round map from {SubSceneManager.AdminForcedMainStation} to {nextMap}.";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");

		SubSceneManager.AdminForcedMainStation = nextMap;
	}

	[Command]
	public void CmdChangeAwaySite(string adminId, string adminToken, string nextAwaySite)
	{
		var admin = PlayerList.Instance.GetAdmin(adminId, adminToken);
		if (admin == null) return;

		if (SubSceneManager.AdminForcedAwaySite == nextAwaySite) return;

		var msg =
			$"{PlayerList.Instance.GetByUserID(adminId).Username}: Changed the next round away site from {SubSceneManager.AdminForcedAwaySite} to {nextAwaySite}.";

		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, null);
		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAdminLogURL, msg, "");

		SubSceneManager.AdminForcedAwaySite = nextAwaySite;
	}

	#endregion
}

public class ServerAuthorityMessageClient : ClientMessage
{
	public string AdminId;
	public string AdminToken;

	public override void Process()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;

		//server stuff
		AdminCommandsManager.Instance.GetComponent<NetworkIdentity>().AssignClientAuthority(SentByPlayer.Connection);
	}

	public static ServerAuthorityMessageClient Send(string adminId, string adminToken)
	{
		ServerAuthorityMessageClient msg = new ServerAuthorityMessageClient
		{
			AdminId = adminId,
			AdminToken = adminToken
		};
		msg.Send();
		return msg;
	}
}