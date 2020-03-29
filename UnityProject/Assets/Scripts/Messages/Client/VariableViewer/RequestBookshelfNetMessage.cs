using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RequestBookshelfNetMessage : ClientMessage
{
	public override short MessageType => (short) MessageTypes.RequestBookshelfNetMessage;
	public ulong BookshelfID;
	public bool IsNewBookshelf = false;
	public string AdminId;
	public string AdminToken;

	public override IEnumerator Process()
	{
		ValidateAdmin();
		yield return null;
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		VariableViewer.RequestSendBookshelf(BookshelfID, IsNewBookshelf);
	}


	public static RequestBookshelfNetMessage Send(ulong _BookshelfID, bool _IsNewBookshelf, string adminId, string adminToken)
	{
		RequestBookshelfNetMessage msg = new RequestBookshelfNetMessage();
		msg.BookshelfID = _BookshelfID;
		msg.IsNewBookshelf = _IsNewBookshelf;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		BookshelfID = reader.ReadUInt64();
		IsNewBookshelf = reader.ReadBoolean();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt64(BookshelfID);
		writer.WriteBoolean(IsNewBookshelf);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}
}