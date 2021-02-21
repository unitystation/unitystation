using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestBookshelfNetMessage : ClientMessage
{
	public struct RequestBookshelfNetMessageNetMessage : NetworkMessage
	{
		public ulong BookshelfID;
		public bool IsNewBookshelf;
		public string AdminId;
		public string AdminToken;
		public uint TheObjectToView;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestBookshelfNetMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestBookshelfNetMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		ValidateAdmin(newMsg);
	}

	void ValidateAdmin(RequestBookshelfNetMessageNetMessage msg)
	{

		var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
		if (admin == null) return;
		if (msg.TheObjectToView != 0)
		{
			LoadNetworkObject(msg.TheObjectToView);
			if (NetworkObject != null)
			{
				VariableViewer.ProcessTransform(NetworkObject.transform,SentByPlayer.GameObject);
			}
		}
		else
		{
			VariableViewer.RequestSendBookshelf(msg.BookshelfID, msg.IsNewBookshelf,SentByPlayer.GameObject);
		}

	}


	public static RequestBookshelfNetMessageNetMessage Send(ulong _BookshelfID, bool _IsNewBookshelf, string adminId, string adminToken)
	{
		RequestBookshelfNetMessageNetMessage msg = new RequestBookshelfNetMessageNetMessage();
		msg.BookshelfID = _BookshelfID;
		msg.IsNewBookshelf = _IsNewBookshelf;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		new RequestBookshelfNetMessage().Send(msg);
		return msg;
	}

	public static RequestBookshelfNetMessageNetMessage Send(GameObject _TheObjectToView, string adminId, string adminToken)
	{
		RequestBookshelfNetMessageNetMessage msg = new RequestBookshelfNetMessageNetMessage();
		msg.TheObjectToView = _TheObjectToView.NetId();
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		new RequestBookshelfNetMessage().Send(msg);
		return msg;
	}
}
