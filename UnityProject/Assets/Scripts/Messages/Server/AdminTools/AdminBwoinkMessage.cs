using System.Collections;
using Mirror;
using UnityEngine;


public class AdminBwoinkMessage : ServerMessage
{
	public class AdminBwoinkMessageNetMessage : ActualMessage
	{
		public string AdminUID;
		public string Message;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminBwoinkMessageNetMessage;
		if(newMsg == null) return;

		SoundManager.Play(SingletonSOSounds.Instance.Bwoink);
		Chat.AddAdminPrivMsg(newMsg.Message);
	}

	public static AdminBwoinkMessageNetMessage  Send(GameObject recipient, string adminUid, string message)
	{
		AdminBwoinkMessageNetMessage  msg = new AdminBwoinkMessageNetMessage
		{
			AdminUID = adminUid,
			Message = message
		};

		new AdminBwoinkMessage().SendTo(recipient, msg);
		return msg;
	}
}