using Mirror;

namespace Messages.Client
{
	public abstract class ClientMessage : GameMessageBase
	{
		/// <summary>
		/// Player that sent this ClientMessage.
		/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
		/// </summary>
		public ConnectedPlayer SentByPlayer;
		public override void Process<T>(NetworkConnection sentBy, T msg)
		{
			SentByPlayer = PlayerList.Instance.Get( sentBy );
			base.Process(sentBy, msg);
		}

		public void Send(NetworkMessage msg)
		{
			NetworkClient.Send(msg, 0);
		}

		public void SendUnreliable(NetworkMessage msg)
		{
			NetworkClient.Send(msg, 1);
		}

		private static uint LocalPlayerId()
		{
			return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
		}
	}
}
