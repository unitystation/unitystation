using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use admin only features. A valid admin token is required
/// to use admin tools.
/// </summary>
public class AdminEnableMessage : ServerMessage
{
	public class AdminEnableMessageNetMessage : NetworkMessage
	{
		public string AdminToken;
		public uint AdminGhostStorage;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminEnableMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.AdminGhostStorage);
		AdminManager.Instance.LocalAdminGhostStorage = NetworkObject.GetComponent<ItemStorage>();
		PlayerList.Instance.SetClientAsAdmin(newMsg.AdminToken);
		UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
	}

	public static AdminEnableMessageNetMessage Send(ConnectedPlayer player, string adminToken)
	{
		UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
		var adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);
		AdminEnableMessageNetMessage msg = new AdminEnableMessageNetMessage
		{
			AdminToken = adminToken,
			AdminGhostStorage = adminGhostItemStorage.GetComponent<NetworkIdentity>().netId
		};

		new AdminEnableMessage().SendTo(player.Connection, msg);

		return msg;
	}
}