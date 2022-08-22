using System.Collections.Generic;
using Mirror;


namespace Messages.Client.VariableViewer
{
	public class RequestInvokeFunction  : ClientMessage<RequestInvokeFunction.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong PageID;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestInvokeFunction(msg.PageID, SentByPlayer.GameObject, SentByPlayer.UserId);
		}

		public static NetMessage Send(ulong _PageID)
		{
			NetMessage msg = new NetMessage
			{
				PageID = _PageID
			};

			Send(msg);
			return msg;
		}
	}
}
