using Mirror;
using US13.Messages.Server;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin.Logs
{
	public class UpdateLogFilePagesNumber : ServerMessage<UpdateLogFilePagesNumber.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int PageNumber;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.AdminLogsWindow.UpdateAvaliablePagesNumber(msg.PageNumber);
		}
	}
}