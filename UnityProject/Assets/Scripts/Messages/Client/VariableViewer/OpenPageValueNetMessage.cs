using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class OpenPageValueNetMessage : ClientMessage
{
	public struct OpenPageValueNetMessageNetMessage : NetworkMessage
	{
		public ulong PageID;
		public uint SentenceID;
		public bool ISSentence;
		public bool iskey;
		public string AdminId;
		public string AdminToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public OpenPageValueNetMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as OpenPageValueNetMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		ValidateAdmin(newMsg);
	}

	void ValidateAdmin(OpenPageValueNetMessageNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
		if (admin == null) return;
		VariableViewer.RequestOpenPageValue(msg.PageID, msg.SentenceID, msg.ISSentence, msg.iskey, SentByPlayer.GameObject);
	}

	public static OpenPageValueNetMessageNetMessage Send(ulong _PageID, uint _SentenceID, string adminId, string adminToken,
		bool Sentenceis = false, bool _iskey = false)
	{
		OpenPageValueNetMessageNetMessage msg = new OpenPageValueNetMessageNetMessage();
		msg.PageID = _PageID;
		msg.SentenceID = _SentenceID;
		msg.ISSentence = Sentenceis;
		msg.iskey = _iskey;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		new OpenPageValueNetMessage().Send(msg);
		return msg;
	}
}
