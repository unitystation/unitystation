using System.Collections;
using Mirror;
using UnityEngine;


public class MentorBwoinkMessage : ServerMessage
{
	public string MentorUID;
	public string Message;

	public override void Process()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Bwoink);
		Chat.AddMentorPrivMsg(Message);
	}

	public static MentorBwoinkMessage  Send(GameObject recipient, string mentorUid, string message)
	{
		MentorBwoinkMessage  msg = new MentorBwoinkMessage
		{
			MentorUID = mentorUid,
			Message = message
		};

		msg.SendTo(recipient);

		return msg;
	}
}