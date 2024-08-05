using System.Collections.Generic;
using Initialisation;
using Messages.Server;
using Mirror;

namespace Messages.Client.Admin.Logs
{
	public class UpdateLogFileNamesDropdown : ServerMessage<UpdateLogFileNamesDropdown.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public List<string> FileNames;
		}

		public override void Process(NetMessage msg)
		{
			LoadManager.DoInMainThread(() => UIManager.Instance.AdminLogsWindow.UpdateLogFileDropdown(msg.FileNames));
		}
	}
}