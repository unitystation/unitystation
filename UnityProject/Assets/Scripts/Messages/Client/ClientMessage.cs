using Mirror;

namespace Messages.Client
{
	public abstract class ClientMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		/// <summary>
		/// Player that sent this ClientMessage.
		/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
		/// </summary>
		public PlayerInfo SentByPlayer;
		public override void Process(NetworkConnection sentBy, T msg)
		{
			SentByPlayer = PlayerList.Instance.GetOnline(sentBy);
			try
			{
				base.Process(sentBy, msg);
			}
			finally
			{
				SentByPlayer = null;
			}
		}

		public static void Send(T msg)
		{
			NetworkClient.Send(msg, 0);
		}

		public static void SendUnreliable(T msg)
		{
			NetworkClient.Send(msg, 1);
		}

		internal bool IsFromAdmin()
		{
			return CustomNetworkManager.IsServer
					? SentByPlayer.IsAdmin
					: PlayerList.Instance.IsClientAdmin;
		}

		private static uint LocalPlayerId()
		{
			return PlayerManager.LocalPlayerObject.GetComponent<NetworkIdentity>().netId;
		}
	}
}
