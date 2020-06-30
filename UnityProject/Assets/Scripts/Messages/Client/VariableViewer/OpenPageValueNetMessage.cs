using System.Collections;
using System.Collections.Generic;
using Mirror;
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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		PageID = reader.ReadUInt64();
		SentenceID = reader.ReadUInt32();
		ISSentence = reader.ReadBoolean();
		iskey = reader.ReadBoolean();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt64(PageID);
		writer.WriteUInt32(SentenceID);
		writer.WriteBoolean(ISSentence);
		writer.WriteBoolean(iskey);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}