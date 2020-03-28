using System.Collections;
using Mirror;
using UnityEngine;


public class AdminBwoinkMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.AdminBwoinkMessage;
	public string AdminUID;
	public string Message;

	public override IEnumerator Process()
	{
		yield return null;
		SoundManager.Play("Bwoink");
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