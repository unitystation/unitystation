using DatabaseAPI;
using Messages.Server.Lobby;
using Mirror;

namespace Messages.Client.Lobby
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
				}
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