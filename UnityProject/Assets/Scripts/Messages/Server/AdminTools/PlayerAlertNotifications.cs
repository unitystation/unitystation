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
	public int Amount;
	public bool IsFullUpdate;

	public override void Process()
	{
		if (!IsFullUpdate)
		{
			UIManager.Instance.playerAlerts.UpdateNotifications(Amount);
		}
		else
		{
			UIManager.Instance.playerAlerts.ClearAllNotifications();
			UIManager.Instance.playerAlerts.UpdateNotifications(Amount);
		}
	}

	/// <summary>
	/// Send notification updates to all admins
	/// </summary>
	public static PlayerAlertNotifications SendToAll(int amt)
	{
		PlayerAlertNotifications msg = new PlayerAlertNotifications
		{
			Amount = amt,
			IsFullUpdate = false,
		};
		msg.SendToAll();
		return msg;
	}

	/// <summary>
	/// Send full update to an admin client
	/// </summary>
	public static PlayerAlertNotifications Send(NetworkConnection adminConn, int amt)
	{
		PlayerAlertNotifications msg = new PlayerAlertNotifications
		{
			Amount = amt,
			IsFullUpdate = true
		};
		msg.SendTo(adminConn);
		return msg;
	}
}