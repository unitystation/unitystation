using Mirror;
using US13.UI.Systems;

namespace US13.Messages.Client.Admin
{
	public class AdminChatRequestRounds : ClientMessage<AdminChatRequestRounds.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerId;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.PLAYER_AHELP))
			{
				UIManager.Instance.adminChatWindows.adminPlayerChat.ServerGetMessageRound(msg.PlayerId, SentByPlayer.Connection);

			}
		}

		public static NetMessage Send(string playerId)
		{
			NetMessage msg = new NetMessage
			{
				PlayerId = playerId,
			};

			Send(msg);
			return msg;
		}
	}
}

