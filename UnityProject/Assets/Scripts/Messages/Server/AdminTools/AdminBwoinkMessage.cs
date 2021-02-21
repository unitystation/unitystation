using System.Collections;
using Mirror;
using UnityEngine;


public class AdminBwoinkMessage : ServerMessage
{
	public class AdminBwoinkMessageNetMessage : NetworkMessage
	{
		public string AdminUID;
		public string Message;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminBwoinkMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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