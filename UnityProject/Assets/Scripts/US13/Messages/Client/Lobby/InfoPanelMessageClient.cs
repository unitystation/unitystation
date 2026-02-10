using Mirror;
using US13.Core.Database;
using US13.Managers;
using US13.Messages.Server.Lobby;

namespace US13.Messages.Client.Lobby
{
	public class InfoPanelMessageClient : ClientMessage<InfoPanelMessageClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
		}

		public override void Process(NetMessage msg)
		{
			InfoPanelMessageServer.Send(
				SentByPlayer.Connection,
				new InfoPanelMessageServer.MotdPageData
				{
					ServerName = ServerData.MotdData.ServerName,
					ServerDescription = ServerData.MotdData.ServerDescription,
					DiscordId = ServerData.MotdData.DiscordLink,
				},
				new InfoPanelMessageServer.RulesPageData
				{
					Rules = ServerData.RulesData
				},
				GameManager.Instance.CurrentRoundState
			);
		}

		public static void Send()
		{
			if (NetworkClient.active == false) return;

			var msg = new NetMessage();
			Send(msg);
		}
	}
}