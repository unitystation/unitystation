using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestChangeVariableNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestChangeVariableNetMessage;
	public string newValue;
	public ulong PageID;
	public bool IsNewBookshelf = false;

	public override IEnumerator Process()
	{
		VariableViewer.RequestChangeVariable(PageID,newValue);
		yield return null;
	}


	public static RequestChangeVariableNetMessage Send(ulong _PageID, string _newValue)
	{
		RequestChangeVariableNetMessage msg = new RequestChangeVariableNetMessage();
		msg.PageID = _PageID;
		msg.newValue = _newValue;

		msg.Send();
		return msg;
	}
}
