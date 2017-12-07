using System.Collections.Generic;

namespace UI
{
	public static class IconConstants
	{
		public static readonly Dictionary<ChatChannel, string> ChatPanelIcons
			= new Dictionary<ChatChannel, string>
		{
			{ ChatChannel.Local, "" },
			{ ChatChannel.OOC, "" },
			{ ChatChannel.Binary, "" },
			{ ChatChannel.Cargo, "" },
			{ ChatChannel.CentComm, "" },
			{ ChatChannel.Command, "" },
			{ ChatChannel.Common, "" },
			{ ChatChannel.Engineering, "" },
			{ ChatChannel.Medical, "" },
			{ ChatChannel.Science, "" },
			{ ChatChannel.Service, "" },
			{ ChatChannel.Syndicate, "" },
			{ ChatChannel.Security, "" },
		};
	}
}