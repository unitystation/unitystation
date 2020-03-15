using System.Collections;
using UnityEngine;

/// <summary>
/// Allows the client to use admin only features. A valid admin token is required
/// to use admin tools.
/// </summary>
public class AdminEnableMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminEnableMessage;
	public string AdminToken;

	public override IEnumerator Process()
	{
		yield return null;
		PlayerList.Instance.SetClientAsAdmin(AdminToken);
		UIManager.Instance.adminChatButtons.gameObject.SetActive(true);
	}

	public static AdminEnableMessage Send(GameObject player, string adminToken)
	{
		AdminEnableMessage msg = new AdminEnableMessage {AdminToken = adminToken};

		msg.SendTo(player);

		return msg;
	}
}