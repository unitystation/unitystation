using Core.Admin.Logs.Stores;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class RequestLogFilePagesNumber : ClientMessage<RequestLogFilePagesNumber.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string LogFileName;
		}

		public override void Process(NetMessage msg)
		{
			var number = AdminLogsStorage.GetTotalPages(msg.LogFileName).Result;
			UpdateLogFilePagesNumber.SendTo(SentByPlayer.Connection, new UpdateLogFilePagesNumber.NetMessage { PageNumber = number });
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