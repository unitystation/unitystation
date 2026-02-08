using Mirror;
using US13.Managers;
using US13.Player;

namespace US13.Messages.Client
{
	public abstract class ClientMessage<T> : GameMessageBase<T> where T : struct, NetworkMessage
	{
		/// <summary>
		/// Player that sent this ClientMessage.
		/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
		/// </summary>
		public PlayerInfo SentByPlayer;

		public NetworkConnection SentBy;
		public override void Process(NetworkConnection sentBy, T msg)
		{
			SentByPlayer = PlayerList.Instance.GetOnline(sentBy);
			SentBy = sentBy;
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

		internal bool HasPermission(string PermissionCode, bool Logfailure = true)
		{
			return AdminCommandsManager.HasPermission(SentByPlayer, PermissionCode, Logfailure);
		}

		internal bool HasPermissions( string[] PermissionCodes)
		{
			return AdminCommandsManager.HasPermissions(SentByPlayer, PermissionCodes, true);
		}

		private static uint LocalPlayerId()
		{
			return PlayerManager.LocalPlayerObject.GetComponent<NetworkIdentity>().netId;
		}
	}
}
