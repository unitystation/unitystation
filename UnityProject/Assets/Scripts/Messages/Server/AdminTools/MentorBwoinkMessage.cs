using System.Collections;
using Mirror;
using UnityEngine;


public class MentorBwoinkMessage : ServerMessage
{
	public class MentorBwoinkMessageNetMessage : ActualMessage
	{
		public string MentorUID;
		public string Message;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as MentorBwoinkMessageNetMessage;
		if(newMsg == null) return;

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