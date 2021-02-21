using System.Collections;
using Mirror;
using UnityEngine;


public class MentorBwoinkMessage : ServerMessage
{
	public struct MentorBwoinkMessageNetMessage : NetworkMessage
	{
		public string MentorUID;
		public string Message;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public MentorBwoinkMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MentorBwoinkMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		SoundManager.Play(SingletonSOSounds.Instance.Bwoink);
		Chat.AddMentorPrivMsg(newMsg.Message);
	}

	public static MentorBwoinkMessageNetMessage  Send(GameObject recipient, string mentorUid, string message)
	{
		MentorBwoinkMessageNetMessage  msg = new MentorBwoinkMessageNetMessage
		{
			MentorUID = mentorUid,
			Message = message
		};

		new MentorBwoinkMessage().SendTo(recipient, msg);

		return msg;
	}
}