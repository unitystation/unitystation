using System.Threading.Tasks;
using Mirror;
using US13.Core.Admin.Logs.Stores;
using US13.Core.Initialisation;

namespace US13.Messages.Client.Admin.Logs
{
	public class RequestLogFilePagesNumber : ClientMessage<RequestLogFilePagesNumber.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string LogFileName;
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
			var number = await AdminLogsStorage.GetTotalPages(msg.LogFileName);
			LoadManager.DoInMainThread(() => UpdateLogFilePagesNumber.SendTo(admin, new UpdateLogFilePagesNumber.NetMessage { PageNumber = number }));
		}

		public static NetMessage Send(string logFileName)
		{
			NetMessage msg = new NetMessage
			{
				LogFileName = logFileName,
			};

			Send(msg);
			return msg;
		}
	}
}