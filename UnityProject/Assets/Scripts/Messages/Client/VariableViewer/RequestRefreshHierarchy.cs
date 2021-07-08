using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Messages.Client.VariableViewer
{
	public class RequestRefreshHierarchy : ClientMessage<RequestRefreshHierarchy.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
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

			global::VariableViewer.RequestHierarchy(SentByPlayer.GameObject);

		}


		public static NetMessage Send(string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}

	}
}