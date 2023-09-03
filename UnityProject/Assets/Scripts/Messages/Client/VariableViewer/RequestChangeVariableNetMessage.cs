using Logs;
using Mirror;


namespace Messages.Client.VariableViewer
{
	public class RequestChangeVariableNetMessage : ClientMessage<RequestChangeVariableNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string newValue;
			public ulong PageID;
			public bool IsNewBookshelf;
			public bool SendToClient;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			global::VariableViewer.RequestChangeVariable(
					msg.PageID, msg.newValue, msg.SendToClient, SentByPlayer.GameObject, SentByPlayer.UserId);

			Loggy.Log(
					$"Admin {SentByPlayer.Username} changed variable {msg.PageID} (in VV) with a new value of: {msg.newValue} ",
					Category.Admin);
		}

		public static NetMessage Send(ulong _PageID, string _newValue, bool InSendToClient)
		{
			NetMessage msg = new NetMessage
			{
				PageID = _PageID,
				newValue = _newValue,
				SendToClient = InSendToClient
			};

			Send(msg);
			return msg;
		}
	}
}
