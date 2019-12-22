using System.Collections;
using UnityEngine;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class AdminEnableMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminEnableMessage;
	public string AdminToken;

	public override IEnumerator Process()
	{
		yield return null;
		PlayerList.Instance.SetClientAsAdmin(AdminToken);
	}

	public static AdminEnableMessage Send(GameObject player, string adminToken)
	{
		AdminEnableMessage msg = new AdminEnableMessage {AdminToken = adminToken};

		msg.SendTo(player);

		return msg;
	}
}