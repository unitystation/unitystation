using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Admin.Logs;
using US13.Messages.Server;

namespace US13.Messages.Client.Admin.Logs
{
	public class UpdateRoundLogFilePageEntries : ServerMessage<UpdateRoundLogFilePageEntries.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<LogEntry> Entries;
		}

		public override void Process(NetMessage msg)
		{
		}

		public static void SendTo(NetworkConnection admin, List<LogEntry> LogEntry)
		{
			NetMessage message = new NetMessage()
			{
				Entries = LogEntry,
			};
			SendTo(admin, message);
		}
	}


	public struct NetFriendlyLog
	{
		public DateTime LogTime;
		public LogInfo[] Log;
		public List<AdminActionToTake> AdminActions;
		public Severity LogImportance;
		public LogCategory Category;
	}
	public struct NetFriendlyLogInfo
	{
		public uint CoreObject;
		public string CoreObjectName;
		public uint WasStoredInObject;
		public string WasStoredInObjectName;
		public string WasControlledByPlayer;
		public Vector3 WasAtPositionWorld;
		public string Info;
	}
}