using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use mentor only features. A valid mentor token is required
/// to use mentor tools.
/// </summary>
public class MentorEnableMessage : ServerMessage
{
	public string MentorToken;

	public override void Process()
	{
		PlayerList.Instance.SetClientAsMentor(MentorToken);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
	}

	public static MentorEnableMessage Send(NetworkConnection player, string mentorToken)
	{
		UIManager.Instance.mentorChatButtons.ServerUpdateAdminNotifications(player);
		MentorEnableMessage msg = new MentorEnableMessage {MentorToken = mentorToken};

		msg.SendTo(player);

		return msg;
	}
}