using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RequestBookshelfNetMessage : ClientMessage
{
	public ulong BookshelfID;
	public bool IsNewBookshelf = false;
	public string AdminId;
	public string AdminToken;
	public uint TheObjectToView;

	public override void Process()
	{
		ValidateAdmin();
	}

	void ValidateAdmin()
	{

		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		if (TheObjectToView != 0)
		{
			LoadNetworkObject(TheObjectToView);
			if (NetworkObject != null)
			{
				VariableViewer.ProcessTransform(NetworkObject.transform,SentByPlayer.GameObject);
			}
		}
		else
		{
			VariableViewer.RequestSendBookshelf(BookshelfID, IsNewBookshelf,SentByPlayer.GameObject);
		}

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

	public static RequestBookshelfNetMessage Send(GameObject _TheObjectToView, string adminId, string adminToken)
	{
		RequestBookshelfNetMessage msg = new RequestBookshelfNetMessage();
		msg.TheObjectToView = _TheObjectToView.NetId();
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}

	/*public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		TheObjectToView = reader.ReadUInt32();
		BookshelfID = reader.ReadUInt64();
		IsNewBookshelf = reader.ReadBoolean();
		AdminId = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(TheObjectToView);
		writer.WriteUInt64(BookshelfID);
		writer.WriteBoolean(IsNewBookshelf);
		writer.WriteString(AdminId);
		writer.WriteString(AdminToken);
	}*/
}