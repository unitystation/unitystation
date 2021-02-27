using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class AdminCheckAdminMessages : ClientMessage<AdminCheckAdminMessages.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerId;
			public int CurrentCount;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerGetUnreadMessages(msg.PlayerId, msg.CurrentCount, SentByPlayer.Connection);
		}

		public static NetMessage Send(string playerId, int currentCount)
		{
			NetMessage msg = new NetMessage
			{
				PlayerId = playerId,
				CurrentCount = currentCount
			};

			Send(msg);
			return msg;
		}
	}
}
