using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class OpenPageValueNetMessage : ClientMessage
{
	public ulong PageID;
	public uint SentenceID;
	public bool ISSentence;
	public bool iskey;
	public string AdminId;
	public string AdminToken;

	public override void Process()
	{
		ValidateAdmin();
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		VariableViewer.RequestOpenPageValue(PageID, SentenceID, ISSentence, iskey, SentByPlayer.GameObject);
	}

	public static OpenPageValueNetMessage Send(ulong _PageID, uint _SentenceID, string adminId, string adminToken,
		bool Sentenceis = false, bool _iskey = false)
	{
		OpenPageValueNetMessage msg = new OpenPageValueNetMessage();
		msg.PageID = _PageID;
		msg.SentenceID = _SentenceID;
		msg.ISSentence = Sentenceis;
		msg.iskey = _iskey;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}
}
