using Logs;
using Mirror;
using US13.Messages.Client.Admin;

namespace US13.Messages.Client.VariableViewer
{
	public class RequestChangeVariableNetMessage : ClientMessage<RequestChangeVariableNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string newValue;
			public uint SentenceID;
			public ulong PageID;
			public bool IsNewBookshelf;
			public bool SendToClient;
			public bool iskey;
			public global::US13.Variable_Viewer.BookViewer.VariableViewer.ListModification ListModification;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (HasPermission(TAG.VV_EDIT) == false) return;

			global::US13.Variable_Viewer.BookViewer.VariableViewer.RequestChangeVariable(
					msg.PageID, msg.newValue, msg.SendToClient, SentByPlayer.GameObject, SentByPlayer.AccountId, msg.SentenceID, msg.iskey, msg.ListModification);

			Loggy.Info(
					$"Admin {SentByPlayer.Username} changed variable {msg.PageID} (in VV) with a new value of: {msg.newValue} ",
					Category.Admin);
		}

		public static NetMessage Send(
			ulong _PageID,
			string _newValue,
			bool InSendToClient,
			uint SentenceID,
			bool iskey,
			global::US13.Variable_Viewer.BookViewer.VariableViewer.ListModification ListModification= global::US13.Variable_Viewer.BookViewer.VariableViewer.ListModification.NONE )
		{

			NetMessage msg = new NetMessage
			{
				PageID = _PageID,
				newValue = _newValue,
				SendToClient = InSendToClient,
				ListModification = ListModification,
				SentenceID =  SentenceID,
				iskey = iskey
			};

			Send(msg);
			return msg;
		}
	}
}
