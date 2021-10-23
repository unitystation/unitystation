using System.Collections.Generic;
using Mirror;


namespace Messages.Client.VariableViewer
{
	public class RequestRefreshHierarchy : ClientMessage<RequestRefreshHierarchy.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestHierarchy(SentByPlayer.GameObject);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
