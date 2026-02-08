using Mirror;
using US13.Messages.Client.Admin;

namespace US13.Messages.Client.VariableViewer
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
			if (HasPermission(TAG.VARIABLE_VIEWER) == false) return;

			global::US13.Variable_Viewer.BookViewer.VariableViewer.RequestHierarchy(SentByPlayer.GameObject);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
