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
			public uint TheObjectToView;
			public bool RefreshHierarchy;
			public bool TeleportTo;
		}

		public override void Process(NetMessage msg)
		{
			ValidateAdmin(msg);
		}

		private void ValidateAdmin(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			if (msg.TheObjectToView != 0)
			{
				LoadNetworkObject(msg.TheObjectToView);
				if (NetworkObject != null)
				{
					global::VariableViewer.ProcessTransform(NetworkObject.transform,SentByPlayer.GameObject,msg.RefreshHierarchy, msg.TeleportTo );
				}
			}
			else
			{
				global::VariableViewer.RequestSendBookshelf(msg.BookshelfID, msg.IsNewBookshelf, SentByPlayer.GameObject, msg.TeleportTo);
			}

		}

		public static NetMessage Send(ulong _BookshelfID, bool _IsNewBookshelf, bool TeleportTo)
		{
			NetMessage msg = new NetMessage
			{
				BookshelfID = _BookshelfID,
				IsNewBookshelf = _IsNewBookshelf,
				TeleportTo = TeleportTo
			};

			Send(msg);
			return msg;
		}

		public static NetMessage Send(GameObject _TheObjectToView, bool RefreshHierarchy)
		{
			NetMessage msg = new NetMessage
			{
				TheObjectToView = _TheObjectToView.NetId(),
				RefreshHierarchy = RefreshHierarchy
			};

			Send(msg);
			return msg;
		}
	}
}
