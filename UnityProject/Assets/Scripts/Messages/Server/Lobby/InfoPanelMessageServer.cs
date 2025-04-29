using Mirror;

namespace Messages.Server.Lobby
{
	/// <summary>
	/// Server message to communicate the client the MOTD and rules data from the server
	/// </summary>
	public class InfoPanelMessageServer: ServerMessage<InfoPanelMessageServer.NetMessage>
	{
		public struct NetMessage: NetworkMessage
		{
			public MotdPageData MotdPageData;
			public RulesPageData RulesPageData;
		}

		public struct MotdPageData
		{
			public string ServerName;
			public string ServerDescription;
			public string DiscordId;
		}

		public struct RulesPageData
		{
			public string Rules;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.ServerInfoPanelWindow.MotdPage.PopulatePage(
				msg.MotdPageData.ServerName,
				msg.MotdPageData.ServerDescription,
				msg.MotdPageData.DiscordId
			);

			UIManager.Instance.ServerInfoPanelWindow.RulesPage.PopulatePage(
				msg.RulesPageData.Rules
			);

			UIManager.Instance.ServerInfoPanelWindow.RefreshWindow();
		}

		public static NetMessage Send(NetworkConnection clientConn, MotdPageData motdPageData, RulesPageData rulesPageData)
		{
			var msg = new NetMessage
			{
				MotdPageData = motdPageData,
				RulesPageData = rulesPageData
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}
}