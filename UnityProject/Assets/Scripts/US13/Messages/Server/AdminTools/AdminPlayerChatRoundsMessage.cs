using Mirror;
using US13.UI.Systems;

namespace US13.Messages.Server.AdminTools
{
	public class AdminPlayerChatRoundsMessage  : ServerMessage<AdminPlayerChatRoundsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string[] Rounds;
			public string playerId;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.adminChatWindows.adminPlayerChat.ClientUpdateAvailableRounds( msg.playerId, msg.Rounds);
		}

		public static NetMessage SendAvailableRoundsToAdmin(NetworkConnection requestee, string playerId, string[] Rounds)
		{
			NetMessage msg =
				new NetMessage
				{
					Rounds = Rounds,
					playerId = playerId
				};

			SendTo(requestee, msg);
			return msg;
		}
	}
}

