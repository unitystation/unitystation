using System.Collections.Generic;
using Core.Admin.Logs;
using Core.Admin.Logs.Stores;
using Initialisation;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class RequestLogFilePageEntries : ClientMessage<RequestLogFilePageEntries.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int PageToRequest;
			public string LogFileName;
		}

		public override void Process(NetMessage msg)
		{
			List<LogEntry> entries = AdminLogsStorage.FetchLogsPaginated(msg.LogFileName, msg.PageToRequest).Result;
			UpdateLogFilePageEntries.NetMessage message = new UpdateLogFilePageEntries.NetMessage()
			{
				Entries = entries,
			};
			LoadManager.DoInMainThread(() => UpdateLogFilePageEntries.SendTo(SentByPlayer.Connection, message));
		}

		public static NetMessage Send(int page, string logFileName)
		{
			NetMessage msg = new NetMessage
			{
				LogFileName = logFileName,
				PageToRequest = page
			};

			Send(msg);
			return msg;
		}
	}
}