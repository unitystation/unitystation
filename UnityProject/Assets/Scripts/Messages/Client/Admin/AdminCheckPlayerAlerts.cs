using Mirror;


namespace Messages.Client.Admin
{
	public class AdminCheckPlayerAlerts : ClientMessage<AdminCheckPlayerAlerts.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int CurrentCount;
		}

		public override void Process(NetMessage msg)
		{
			UIManager.Instance.playerAlerts.ServerRequestEntries(SentByPlayer.UserId, msg.CurrentCount, SentByPlayer.Connection);
		}

		public static NetMessage Send(int currentCount)
		{
			NetMessage msg = new NetMessage
			{
				CurrentCount = currentCount
			};

			Send(msg);
			return msg;
		}
	}
}
