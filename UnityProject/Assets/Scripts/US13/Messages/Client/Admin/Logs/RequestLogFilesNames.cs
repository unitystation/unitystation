using Mirror;
using US13.Core.Admin.Logs.Stores;

namespace US13.Messages.Client.Admin.Logs
{
	public class RequestLogFilesNames : ClientMessage<RequestLogFilesNames.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_LOGS) == false)
			{
				return;
			}

			var files = AdminLogsStorage.GetAllLogFiles();
			UpdateLogFileNamesDropdown.SendTo(SentByPlayer.Connection, new UpdateLogFileNamesDropdown.NetMessage { FileNames = files });
		}
	}
}