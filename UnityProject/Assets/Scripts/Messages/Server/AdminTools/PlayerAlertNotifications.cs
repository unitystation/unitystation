using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

/// <summary>
/// Notify the admins when a alert comes in!!
/// </summary>
public class PlayerAlertNotifications : ServerMessage
{
	public class PlayerAlertNotificationsNetMessage : NetworkMessage
	{
		public int Amount;
		public bool IsFullUpdate;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as PlayerAlertNotificationsNetMessage;
		if(newMsg == null) return;

		if (!newMsg.IsFullUpdate)
		{
			UIManager.Instance.playerAlerts.UpdateNotifications(newMsg.Amount);
		}
		else
		{
			UIManager.Instance.playerAlerts.ClearAllNotifications();
			UIManager.Instance.playerAlerts.UpdateNotifications(newMsg.Amount);
		}
	}

	/// <summary>
	/// Send notification updates to all admins
	/// </summary>
	public static PlayerAlertNotificationsNetMessage SendToAll(int amt)
	{
		PlayerAlertNotificationsNetMessage msg = new PlayerAlertNotificationsNetMessage
		{
			Amount = amt,
			IsFullUpdate = false,
		};
		new PlayerAlertNotifications().SendToAll(msg);
		return msg;
	}

	/// <summary>
	/// Send full update to an admin client
	/// </summary>
	public static PlayerAlertNotificationsNetMessage Send(NetworkConnection adminConn, int amt)
	{
		PlayerAlertNotificationsNetMessage msg = new PlayerAlertNotificationsNetMessage
		{
			Amount = amt,
			IsFullUpdate = true
		};
		new PlayerAlertNotifications().SendTo(adminConn, msg);
		return msg;
	}
}