using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class OpenBookIDNetMessage : ClientMessage
{
	public ulong BookID;
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
		VariableViewer.RequestSendBook(BookID, SentByPlayer.GameObject);
	}


	public static OpenBookIDNetMessage Send(ulong BookID, string adminId, string adminToken)
	{
		OpenBookIDNetMessage msg = new OpenBookIDNetMessage();
		msg.BookID = BookID;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		BookID = reader.ReadUInt64();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt64(BookID);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}