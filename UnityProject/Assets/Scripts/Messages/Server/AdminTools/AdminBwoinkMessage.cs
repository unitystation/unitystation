using System.Collections;
using Mirror;
using UnityEngine;


public class AdminBwoinkMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminBwoinkMessage;
	public string AdminUID;
	public string Message;
	public uint Recipient;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		Debug.Log("////TODO: MAKE IT GO BWOINK AND POP UP ON USER SCREEN");
		SoundManager.Play("Bwoink");
		
	}

	public static AdminBwoinkMessage  Send(GameObject recipient, string adminUid, string message)
	{
		AdminBwoinkMessage  msg = new AdminBwoinkMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			AdminUID = adminUid,
			Message = message
		};

		msg.SendTo(recipient);

		return msg;
	}
}