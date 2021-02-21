using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use mentor only features. A valid mentor token is required
/// to use mentor tools.
/// </summary>
public class MentorEnableMessage : ServerMessage
{
	public class MentorEnableMessageNetMessage : NetworkMessage
	{
		public string MentorToken;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as MentorEnableMessageNetMessage;
		if(newMsg == null) return;

		PlayerList.Instance.SetClientAsMentor(newMsg.MentorToken);
		UIManager.Instance.mentorChatButtons.transform.parent.gameObject.SetActive(true);
	}

	public static MentorEnableMessageNetMessage Send(NetworkConnection player, string mentorToken)
	{
		UIManager.Instance.mentorChatButtons.ServerUpdateAdminNotifications(player);
		MentorEnableMessageNetMessage msg = new MentorEnableMessageNetMessage {MentorToken = mentorToken};

		new MentorEnableMessage().SendTo(player, msg);
		return msg;
	}
}