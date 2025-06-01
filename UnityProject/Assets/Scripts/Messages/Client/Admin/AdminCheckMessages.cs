using Messages.Client;
using Mirror;

namespace Messages.Client.Admin
{
	public class AdminCheckMessages : ClientMessage<AdminCheckMessages.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string PlayerId;
			public int CurrentCount;
			public int RoundID;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.PLAYER_AHELP))
			{
				UIManager.Instance.adminChatWindows.adminPlayerChat.ServerGetUnreadMessages(msg.PlayerId, msg.CurrentCount, msg.RoundID, SentByPlayer.Connection);
			}
		}

		public static NetMessage Send(string playerId, int currentCount, int RoundID)
		{
			NetMessage msg = new NetMessage
			{
				PlayerId = playerId,
				CurrentCount = currentCount,
				RoundID = RoundID
			};

			Send(msg);
			return msg;
		}
	}
}
