using Mirror;
using UnityEngine;

namespace Messages.Client.VariableViewer
{
	public class RequestToViewObjectsAtTile : ClientMessage<RequestToViewObjectsAtTile.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Vector3 Location;
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

			global::VariableViewer.ProcessTile(msg.Location, SentByPlayer.GameObject);
		}

		public static NetMessage Send(Vector3 _Location, string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.Location = _Location;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}
	}
}
