using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use admin only features. A valid admin token is required
/// to use admin tools.
/// </summary>
public class AdminEnableMessage : ServerMessage
{
	public string AdminToken;
	public uint AdminGhostStorage;
	public override void Process()
	{
		LoadNetworkObject(AdminGhostStorage);
		AdminManager.Instance.LocalAdminGhostStorage = NetworkObject.GetComponent<ItemStorage>();
		PlayerList.Instance.SetClientAsAdmin(AdminToken);
		UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
	}

	public static AdminEnableMessage Send(ConnectedPlayer player, string adminToken)
	{
		UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player.Connection);
		var adminGhostItemStorage = AdminManager.Instance.GetItemSlotStorage(player);
		AdminEnableMessage msg = new AdminEnableMessage
		{
			AdminToken = adminToken,
			AdminGhostStorage = adminGhostItemStorage.GetComponent<NetworkIdentity>().netId
		};

		msg.SendTo(player.Connection);

		return msg;
	}
}