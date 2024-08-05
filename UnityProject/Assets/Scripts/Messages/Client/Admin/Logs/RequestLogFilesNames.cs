using Core.Admin.Logs.Stores;
using Initialisation;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class RequestLogFilesNames : ClientMessage<RequestLogFilesNames.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			var files = AdminLogsStorage.GetAllLogFiles();
			UpdateLogFileNamesDropdown.SendTo(SentByPlayer.Connection, new UpdateLogFileNamesDropdown.NetMessage { FileNames = files });
		}
	}
}