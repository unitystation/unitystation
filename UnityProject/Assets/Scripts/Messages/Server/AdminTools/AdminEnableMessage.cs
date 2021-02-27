using Mirror;

namespace Messages.Server.AdminTools
{
	/// <summary>
	/// Allows the client to use admin only features. A valid admin token is required
	/// to use admin tools.
	/// </summary>
	public class AdminEnableMessage : ServerMessage<AdminEnableMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string AdminToken;
			public uint AdminGhostStorage;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.AdminGhostStorage);
			AdminManager.Instance.LocalAdminGhostStorage = NetworkObject.GetComponent<ItemStorage>();
			PlayerList.Instance.SetClientAsAdmin(msg.AdminToken);
			UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
			UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
		}

		public static NetMessage Send(ConnectedPlayer player, string adminToken)
		{
			UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
			var adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);
			NetMessage msg = new NetMessage
			{
				AdminToken = adminToken,
				AdminGhostStorage = adminGhostItemStorage.GetComponent<NetworkIdentity>().netId
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}