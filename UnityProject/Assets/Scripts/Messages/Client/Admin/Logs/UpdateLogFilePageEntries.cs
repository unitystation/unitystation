using System.Collections.Generic;
using Core.Admin.Logs;
using Initialisation;
using Messages.Server;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class UpdateLogFilePageEntries : ServerMessage<UpdateLogFilePageEntries.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<StoredLogEntry> Entries;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.AdminLogsWindow.UpdateLogEntries(msg.Entries);
		}

		public static void SendTo(NetworkConnection admin, List<StoredLogEntry> LogEntry)
		{
			NetMessage message = new NetMessage()
			{
				Entries = LogEntry,
			};
			SendTo(admin, message);
		}
	}
}