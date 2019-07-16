using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenBookIDNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestOpenBookIDNetMessage;
	public ulong BookID;

	public override IEnumerator Process()
	{
		//var livingHealthBehaviour = SentByPlayer.Script.GetComponent<LivingHealthBehaviour>();
		VariableViewer.RequestSendBook(BookID);
		yield return null;
	}


	public static OpenBookIDNetMessage Send(ulong BookID)
	{
		OpenBookIDNetMessage msg = new OpenBookIDNetMessage();
		msg.BookID = BookID;
		msg.Send();
		return msg;
	}

}
