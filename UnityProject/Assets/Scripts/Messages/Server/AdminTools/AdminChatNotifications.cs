using System;
using System.Collections;
using System.Collections.Generic;
using AdminTools;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server.AdminTools
{
	/// <summary>
	/// Notify the admins when a message comes in!!
	/// </summary>
	public class AdminChatNotifications : ServerMessage<AdminChatNotifications.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string NotificationKey;
			public AdminChatWindow TargetWindow;
			public int Amount;
			public bool ClearAll;
			public bool IsFullUpdate;
			public string FullUpdateJson;
		}

		public override void Process(NetMessage msg)
		{
			if (!msg.IsFullUpdate)
			{
				UIManager.Instance.adminChatButtons.ClientUpdateNotifications(msg.NotificationKey, msg.TargetWindow,
					msg.Amount, msg.ClearAll);
				UIManager.Instance.mentorChatButtons.ClientUpdateNotifications(msg.NotificationKey, msg.TargetWindow,
					msg.Amount, msg.ClearAll);
				UIManager.Instance.prayerChatButtons.ClientUpdateNotifications(msg.NotificationKey, msg.TargetWindow,
					msg.Amount, msg.ClearAll);
			}
			else
			{
				UIManager.Instance.adminChatButtons.ClearAllNotifications();
				UIManager.Instance.mentorChatButtons.ClearAllNotifications();
				UIManager.Instance.prayerChatButtons.ClearAllNotifications();
				var notiUpdate = JsonConvert.DeserializeObject<AdminChatNotificationFullUpdate>(msg.FullUpdateJson);

				foreach (var n in notiUpdate.notificationEntries)
				{
					UIManager.Instance.adminChatButtons.ClientUpdateNotifications(n.Key, n.TargetWindow,
						n.Amount, false);
					UIManager.Instance.mentorChatButtons.ClientUpdateNotifications(n.Key, n.TargetWindow,
						n.Amount, false);
					UIManager.Instance.prayerChatButtons.ClientUpdateNotifications(n.Key, n.TargetWindow,
						n.Amount, false);
				}
			}
		}

		/// <summary>
		/// Send notification updates to all admins
		/// </summary>
		public static NetMessage SendToAll(string notificationKey, AdminChatWindow targetWindow,
			int amt, bool clearAll = false)
		{
			NetMessage msg = new NetMessage
			{
				NotificationKey = notificationKey,
				TargetWindow = targetWindow,
				Amount = amt,
				ClearAll = clearAll,
				IsFullUpdate = false,
				FullUpdateJson = ""
			};

			SendToAll(msg);
			return msg;
		}

		/// <summary>
		/// Send full update to an admin client
		/// </summary>
		public static NetMessage Send(NetworkConnection adminConn, AdminChatNotificationFullUpdate update)
		{
			NetMessage msg = new NetMessage
			{
				IsFullUpdate = true,
				FullUpdateJson = JsonConvert.SerializeObject(update)
			};

			SendTo(adminConn, msg);
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
}