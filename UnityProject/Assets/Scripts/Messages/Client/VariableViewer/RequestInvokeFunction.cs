using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Client.VariableViewer
{
	public class RequestInvokeFunction  : ClientMessage<RequestInvokeFunction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong PageID;
			public string AdminId;
			public string AdminToken;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		void ValidateAdmin(NetMessage msg)
		{
			var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
			if (admin == null) return;

			global::VariableViewer.RequestInvokeFunction(msg.PageID, SentByPlayer.GameObject, msg.AdminId);
		}

		public static NetMessage Send(ulong _PageID, string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.PageID = _PageID;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}
	}
}