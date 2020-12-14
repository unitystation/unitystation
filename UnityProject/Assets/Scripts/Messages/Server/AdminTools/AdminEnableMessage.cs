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

	public override void Process()
	{
		PlayerList.Instance.SetClientAsAdmin(AdminToken);
		UIManager.Instance.adminChatButtons.transform.parent.gameObject.SetActive(true);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
	}

	public static AdminEnableMessage Send(NetworkConnection player, string adminToken)
	{
		UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player);
		AdminEnableMessage msg = new AdminEnableMessage {AdminToken = adminToken};

		msg.SendTo(player);

		return msg;
	}
}