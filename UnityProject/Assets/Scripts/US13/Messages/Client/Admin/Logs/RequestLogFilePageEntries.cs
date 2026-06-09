using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using US13.Core.Admin.Logs;
using US13.Core.Admin.Logs.Stores;
using US13.Core.Initialisation;

namespace US13.Messages.Client.Admin.Logs
{
	public class RequestLogFilePageEntries : ClientMessage<RequestLogFilePageEntries.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int PageToRequest;
			public string LogFileName;
			public string SearchString;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_LOGS))
			{
				_ = Do(msg, SentByPlayer.Connection);
			}
		}

		private async Task Do(NetMessage msg, NetworkConnectionToClient admin)
		{

			List<StoredLogEntry> entries = await AdminLogsStorage.FetchLogsPaginated(msg.LogFileName, msg.PageToRequest, msg.SearchString);
			LoadManager.DoInMainThread(() => UpdateLogFilePageEntries.SendTo(admin, entries ));
		}

		public static NetMessage Send(int page, string logFileName, string SearchString)
		{
			NetMessage msg = new NetMessage
			{
				LogFileName = logFileName,
				PageToRequest = page,
				SearchString = SearchString
			};

			Send(msg);
			return msg;
		}
	}
}