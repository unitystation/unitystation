using System;
using System.Collections.Generic;
using System.Linq;
using AdminTools;
using Messages.Server;
using Mirror;
using UnityEngine;

public class AdminReturnPerformancesStatistics : ServerMessage<AdminReturnPerformancesStatistics.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public PerformanceManager.PerformanceInfo Info;



	}

	public override void Process(NetMessage msg)
	{
		GUI_AdminTools.Instance.SetServerPerformancePage(msg.Info);
	}



	public static NetMessage Send(PlayerInfo player, PerformanceManager.PerformanceInfo InInfo)
	{
		NetMessage msg = new NetMessage
		{
			Info = InInfo
		};

		SendTo(player.Connection, msg);
		return msg;
	}
}