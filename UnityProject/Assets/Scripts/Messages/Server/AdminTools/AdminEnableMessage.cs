using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use admin only features. A valid admin token is required
/// to use admin tools.
/// </summary>
public class AdminEnableMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.AdminEnableMessage;
	public string AdminToken;

	public override IEnumerator Process()
	{
		yield return null;
		PlayerList.Instance.SetClientAsAdmin(AdminToken);
		UIManager.Instance.adminChatButtons.gameObject.SetActive(true);
	}

	public static AdminEnableMessage Send(NetworkConnection player, string adminToken)
	{
		UIManager.Instance.adminChatButtons.ServerUpdateAdminNotifications(player);
		AdminEnableMessage msg = new AdminEnableMessage {AdminToken = adminToken};

		msg.SendTo(player);

		return msg;
	}
}