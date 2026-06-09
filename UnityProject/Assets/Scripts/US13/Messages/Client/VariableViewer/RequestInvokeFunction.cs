using Mirror;
using US13.Messages.Client.Admin;

namespace US13.Messages.Client.VariableViewer
{
	public class RequestInvokeFunction  : ClientMessage<RequestInvokeFunction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong PageID;
			public bool SendToClient;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (HasPermission(TAG.VV_CALL) == false) return;

			global::US13.Variable_Viewer.BookViewer.VariableViewer.RequestInvokeFunction(msg.PageID,msg.SendToClient ,  SentByPlayer.GameObject, SentByPlayer.AccountId);
		}

		public static NetMessage Send(ulong _PageID, bool InSendToClient)
		{
			NetMessage msg = new NetMessage
			{
				PageID = _PageID,
				SendToClient =  InSendToClient,
			};

			Send(msg);
			return msg;
		}
	}
}
