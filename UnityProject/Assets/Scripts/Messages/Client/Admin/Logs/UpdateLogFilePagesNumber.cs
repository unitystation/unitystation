using Initialisation;
using Messages.Server;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class UpdateLogFilePagesNumber : ServerMessage<UpdateLogFilePagesNumber.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int PageNumber;
		}

		public override void Process(NetMessage msg)
		{
			LoadManager.DoInMainThread(() => UIManager.Instance.AdminLogsWindow.UpdateAvaliablePagesNumber(msg.PageNumber));
		}
	}
}