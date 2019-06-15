using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestList_DictContentsNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestList_DictContentsMessage;
	public ulong PageID;

	public override IEnumerator Process()
	{
		VariableViewer.RequestSendList_Dict(PageID);
		yield return null;
	}


	public static RequestList_DictContentsNetMessage Send(ulong PageID)
	{
		RequestList_DictContentsNetMessage msg = new RequestList_DictContentsNetMessage();
		msg.PageID = PageID;
		msg.Send();
		return msg;
	}

}
