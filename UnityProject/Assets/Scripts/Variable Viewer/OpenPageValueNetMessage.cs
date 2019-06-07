using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenPageValueNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.OpenPageValueNetMessage;
	public ulong PageID;

	public override IEnumerator Process()
	{
		//var livingHealthBehaviour = SentByPlayer.Script.GetComponent<LivingHealthBehaviour>();
		VariableViewer.RequestOpenPageValue(PageID);
		yield return null;
	}


	public static OpenPageValueNetMessage Send(ulong PageID)
	{
		OpenPageValueNetMessage msg = new OpenPageValueNetMessage();
		msg.PageID = PageID;
		msg.Send();
		return msg;
	}

}
