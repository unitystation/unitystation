using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenPageValueNetMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.OpenPageValueNetMessage;
	public ulong PageID;
	public uint SentenceID;
	public bool ISSentence;
	public bool iskey;

	public override IEnumerator Process()
	{
		//var livingHealthBehaviour = SentByPlayer.Script.GetComponent<LivingHealthBehaviour>();
		VariableViewer.RequestOpenPageValue(PageID, SentenceID, ISSentence, iskey);
		yield return null;
	}


	public static OpenPageValueNetMessage Send(ulong _PageID,  uint _SentenceID, bool Sentenceis = false , bool _iskey = false)
	{
		OpenPageValueNetMessage msg = new OpenPageValueNetMessage();
		msg.PageID = _PageID;
		msg.SentenceID = _SentenceID;
		msg.ISSentence = Sentenceis;
		msg.iskey = _iskey;
		msg.Send();
		return msg;
	}

}
