using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

/// <summary>
/// Notify the admins when a message comes in!!
/// </summary>
public class AdminChatNotifications : ServerMessage
{
	public string NotificationKey;
	public AdminChatWindow TargetWindow;
	public int Amount;
	public bool ClearAll;
	public bool IsFullUpdate;
	public string FullUpdateJson;

	public override void Process()
	{
		if (!IsFullUpdate)
		{
			UIManager.Instance.adminChatButtons.ClientUpdateNotifications(NotificationKey, TargetWindow,
				Amount, ClearAll);
			UIManager.Instance.mentorChatButtons.ClientUpdateNotifications(NotificationKey, TargetWindow,
				Amount, ClearAll);
		}
		else
		{
			UIManager.Instance.adminChatButtons.ClearAllNotifications();
			UIManager.Instance.mentorChatButtons.ClearAllNotifications();
			var notiUpdate = JsonUtility.FromJson<AdminChatNotificationFullUpdate>(FullUpdateJson);

			foreach (var n in notiUpdate.notificationEntries)
			{
				UIManager.Instance.adminChatButtons.ClientUpdateNotifications(n.Key, n.TargetWindow,
					n.Amount, false);
				UIManager.Instance.mentorChatButtons.ClientUpdateNotifications(n.Key, n.TargetWindow,
					n.Amount, false);
			}
		}
	}

	/// <summary>
	/// Send notification updates to all admins
	/// </summary>
	public static AdminChatNotifications SendToAll(string notificationKey, AdminChatWindow targetWindow,
		int amt, bool clearAll = false)
	{
		AdminChatNotifications msg = new AdminChatNotifications
		{
			NotificationKey = notificationKey,
			TargetWindow = targetWindow,
			Amount = amt,
			ClearAll = clearAll,
			IsFullUpdate = false,
			FullUpdateJson = ""
		};
		msg.SendToAll();
		return msg;
	}

	/// <summary>
	/// Send full update to an admin client
	/// </summary>
	public static AdminChatNotifications Send(NetworkConnection adminConn, AdminChatNotificationFullUpdate update)
	{
		AdminChatNotifications msg = new AdminChatNotifications
		{
			IsFullUpdate = true,
			FullUpdateJson = JsonUtility.ToJson(update)
		};
		msg.SendTo(adminConn);
		return msg;
	}
}

[Serializable]
public class AdminChatNotificationFullUpdate
{
	public List<AdminChatNotificationEntry> notificationEntries = new List<AdminChatNotificationEntry>();
}

[Serializable]
public class AdminChatNotificationEntry
{
	public string Key;
	public int Amount;
	public AdminChatWindow TargetWindow;
}