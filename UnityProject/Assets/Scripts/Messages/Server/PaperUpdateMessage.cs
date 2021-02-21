using System.Collections;
using UnityEngine;
using Mirror;

public class PaperUpdateMessage : ServerMessage
{
	public class PaperUpdateMessageNetMessage : NetworkMessage
	{
		public uint PaperToUpdate;
		public uint Recipient;
		public string Message;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as PaperUpdateMessageNetMessage;
		if(newMsg == null) return;

		LoadMultipleObjects(new uint[] {newMsg.Recipient, newMsg.PaperToUpdate});
		var paper = NetworkObjects[1].GetComponent<Paper>();
		paper.PaperString = newMsg.Message;
		ControlTabs.RefreshTabs();
	}

	public static PaperUpdateMessageNetMessage Send(GameObject recipient, GameObject paperToUpdate, string message)
	{
		PaperUpdateMessageNetMessage msg = new PaperUpdateMessageNetMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			PaperToUpdate = paperToUpdate.GetComponent<NetworkIdentity>().netId,
			Message = message
		};
		new PaperUpdateMessage().SendTo(recipient, msg);
		return msg;
	}
}
