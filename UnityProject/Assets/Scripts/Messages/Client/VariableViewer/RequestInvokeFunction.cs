using System.Collections.Generic;
using Mirror;


namespace Messages.Client.VariableViewer
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
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestInvokeFunction(msg.PageID,msg.SendToClient , SentByPlayer.GameObject, SentByPlayer.UserId);
		}

		public static NetMessage Send(ulong _PageID, bool InSendToClient)
		{
			NetMessage msg = new NetMessage
			{
				PageID = _PageID,
				SendToClient =  InSendToClient
			};

			Send(msg);
			return msg;
		}
	}
}
