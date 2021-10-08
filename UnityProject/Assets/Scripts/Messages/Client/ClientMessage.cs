using Mirror;

namespace Messages.Client
{
	public abstract class ClientMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		/// <summary>
		/// Player that sent this ClientMessage.
		/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
		/// </summary>
		public ConnectedPlayer SentByPlayer;
		public override void Process(NetworkConnection sentBy, T msg)
		{
			SentByPlayer = PlayerList.Instance.Get(sentBy);
			base.Process(sentBy, msg);
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
			if (CustomNetworkManager.IsServer)
			{
				return PlayerList.Instance.IsAdmin(SentByPlayer);
			}

			return PlayerList.Instance.IsClientAdmin;
		}

		private static uint LocalPlayerId()
		{
			return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
		}
	}
}
