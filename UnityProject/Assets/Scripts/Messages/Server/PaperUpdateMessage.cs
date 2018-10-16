using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PaperUpdateMessage : ServerMessage
{

	public static short MessageType = (short)MessageTypes.PaperUpdateMessage;

	public NetworkInstanceId PaperToUpdate;
	public NetworkInstanceId Recipient;
	public string Message;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient, PaperToUpdate);
		var paper = NetworkObjects[1].GetComponent<Paper>();
		paper.PaperString = Message;
		ControlTabs.RefreshTabs();
	}

	public static PaperUpdateMessage Send(GameObject recipient, GameObject paperToUpdate, string message)
	{
		PaperUpdateMessage msg = new PaperUpdateMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			PaperToUpdate = paperToUpdate.GetComponent<NetworkIdentity>().netId,
			Message = message
		};
		msg.SendTo(recipient);
		return msg;
	}
}
