using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Allows the client to use mentor only features. A valid mentor token is required
/// to use mentor tools.
/// </summary>
public class MentorEnableMessage : ServerMessage
{
	public struct MentorEnableMessageNetMessage : NetworkMessage
	{
		public string MentorToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public MentorEnableMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MentorEnableMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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