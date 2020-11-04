using System.Collections;
using Mirror;
using UnityEngine;


public class AdminBwoinkMessage : ServerMessage
{
	public string AdminUID;
	public string Message;

	public override void Process()
	{
		SoundManager.Play(SingletonSOSounds.Instance.Bwoink);
		Chat.AddAdminPrivMsg(Message);
	}

	public static AdminBwoinkMessage  Send(GameObject recipient, string adminUid, string message)
	{
		AdminBwoinkMessage  msg = new AdminBwoinkMessage
		{
			AdminUID = adminUid,
			Message = message
		};

		msg.SendTo(recipient);

		return msg;
	}
}