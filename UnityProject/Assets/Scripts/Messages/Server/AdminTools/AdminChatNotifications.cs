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
	public class AdminChatNotificationsNetMessage : NetworkMessage
	{
		public string NotificationKey;
		public AdminChatWindow TargetWindow;
		public int Amount;
		public bool ClearAll;
		public bool IsFullUpdate;
		public string FullUpdateJson;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminChatNotificationsNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (!newMsg.IsFullUpdate)
		{
			UIManager.Instance.adminChatButtons.ClientUpdateNotifications(newMsg.NotificationKey, newMsg.TargetWindow,
				newMsg.Amount, newMsg.ClearAll);
			UIManager.Instance.mentorChatButtons.ClientUpdateNotifications(newMsg.NotificationKey, newMsg.TargetWindow,
				newMsg.Amount, newMsg.ClearAll);
		}
		else
		{
			UIManager.Instance.adminChatButtons.ClearAllNotifications();
			UIManager.Instance.mentorChatButtons.ClearAllNotifications();
			var notiUpdate = JsonUtility.FromJson<AdminChatNotificationFullUpdate>(newMsg.FullUpdateJson);

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
	public static AdminChatNotificationsNetMessage SendToAll(string notificationKey, AdminChatWindow targetWindow,
		int amt, bool clearAll = false)
	{
		AdminChatNotificationsNetMessage msg = new AdminChatNotificationsNetMessage
		{
			NotificationKey = notificationKey,
			TargetWindow = targetWindow,
			Amount = amt,
			ClearAll = clearAll,
			IsFullUpdate = false,
			FullUpdateJson = ""
		};

		new AdminChatNotifications().SendToAll(msg);
		return msg;
	}

	/// <summary>
	/// Send full update to an admin client
	/// </summary>
	public static AdminChatNotificationsNetMessage Send(NetworkConnection adminConn, AdminChatNotificationFullUpdate update)
	{
		AdminChatNotificationsNetMessage msg = new AdminChatNotificationsNetMessage
		{
			IsFullUpdate = true,
			FullUpdateJson = JsonUtility.ToJson(update)
		};

		new AdminChatNotifications().SendTo(adminConn, msg);
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