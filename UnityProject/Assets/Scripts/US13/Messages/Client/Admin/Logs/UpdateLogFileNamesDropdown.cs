using System.Collections.Generic;
using Mirror;
using US13.Messages.Server;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin.Logs
{
	public class UpdateLogFileNamesDropdown : ServerMessage<UpdateLogFileNamesDropdown.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<string> FileNames;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.AdminLogsWindow.UpdateLogFileDropdown(msg.FileNames);
		}
	}
}