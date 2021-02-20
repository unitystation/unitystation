using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class RequestBookshelfNetMessage : ClientMessage
{
	public class RequestBookshelfNetMessageNetMessage : ActualMessage
	{
		public ulong BookshelfID;
		public bool IsNewBookshelf = false;
		public string AdminId;
		public string AdminToken;
		public uint TheObjectToView;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as RequestBookshelfNetMessageNetMessage;
		if(newMsg == null) return;

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
