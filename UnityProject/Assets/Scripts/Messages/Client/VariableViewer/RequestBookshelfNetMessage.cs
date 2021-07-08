using Mirror;
using UnityEngine;

namespace Messages.Client.VariableViewer
{
	public class RequestBookshelfNetMessage : ClientMessage<RequestBookshelfNetMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ulong BookshelfID;
			public bool IsNewBookshelf;
			public string AdminId;
			public string AdminToken;
			public uint TheObjectToView;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		void ValidateAdmin(NetMessage msg)
		{

			var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
			if (admin == null) return;
			if (msg.TheObjectToView != 0)
			{
				LoadNetworkObject(msg.TheObjectToView);
				if (NetworkObject != null)
				{
					global::VariableViewer.ProcessTransform(NetworkObject.transform,SentByPlayer.GameObject);
				}
			}
			else
			{
				global::VariableViewer.RequestSendBookshelf(msg.BookshelfID, msg.IsNewBookshelf,SentByPlayer.GameObject);
			}

		}


		public static NetMessage Send(ulong _BookshelfID, bool _IsNewBookshelf, string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.BookshelfID = _BookshelfID;
			msg.IsNewBookshelf = _IsNewBookshelf;
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}

		public static NetMessage Send(GameObject _TheObjectToView, string adminId, string adminToken)
		{
			NetMessage msg = new NetMessage();
			msg.TheObjectToView = _TheObjectToView.NetId();
			msg.AdminId = adminId;
			msg.AdminToken = adminToken;

			Send(msg);
			return msg;
		}
	}
}
