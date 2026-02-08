using Mirror;
using UnityEngine;
using US13.Messages.Client.Admin;

namespace US13.Messages.Client.VariableViewer
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

		private void ValidateAdmin(NetMessage msg)
		{
			if (HasPermission(TAG.VARIABLE_VIEWER) == false) return;

			global::US13.Variable_Viewer.BookViewer.VariableViewer.ProcessTile(msg.Location, SentByPlayer.GameObject);
		}

		public static NetMessage Send(Vector3 _Location)
		{
			NetMessage msg = new NetMessage();
			msg.Location = _Location;

			Send(msg);
			return msg;
		}
	}
}
