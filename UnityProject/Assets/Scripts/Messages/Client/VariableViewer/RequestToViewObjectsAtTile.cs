using UnityEngine;
using Mirror;


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

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.ProcessTile(msg.Location, SentByPlayer.GameObject);
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
