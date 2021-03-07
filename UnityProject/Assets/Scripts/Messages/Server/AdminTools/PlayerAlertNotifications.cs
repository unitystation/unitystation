using Mirror;

namespace Messages.Server.AdminTools
{
	/// <summary>
	/// Notify the admins when a alert comes in!!
	/// </summary>
	public class PlayerAlertNotifications : ServerMessage<PlayerAlertNotifications.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int Amount;
			public bool IsFullUpdate;
		}

		public override void Process(NetMessage msg)
		{
			if (!msg.IsFullUpdate)
			{
				UIManager.Instance.playerAlerts.UpdateNotifications(msg.Amount);
			}
			else
			{
				UIManager.Instance.playerAlerts.ClearAllNotifications();
				UIManager.Instance.playerAlerts.UpdateNotifications(msg.Amount);
			}
		}

		/// <summary>
		/// Send notification updates to all admins
		/// </summary>
		public static NetMessage SendToAll(int amt)
		{
			NetMessage msg = new NetMessage
			{
				Amount = amt,
				IsFullUpdate = false,
			};

			SendToAll(msg);
			return msg;
		}

		/// <summary>
		/// Send full update to an admin client
		/// </summary>
		public static NetMessage Send(NetworkConnection adminConn, int amt)
		{
			NetMessage msg = new NetMessage
			{
				Amount = amt,
				IsFullUpdate = true
			};

			SendTo(adminConn, msg);
			return msg;
		}
	}
}