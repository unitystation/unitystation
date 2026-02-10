using Mirror;
using SecureStuff;
using US13.Core.Sprite_Handler;
using US13.Messages.Server.VariableViewer;

namespace US13.Messages.Client.Admin
{
	public class RequestRenameVVObject : ClientMessage<RequestRenameVVObject.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string NewName;
			public ulong VVObjectID;
			public bool NetworkToClients;
		}

		public override void Process(NetMessage msg)
		{
			VerifyAdminStatus(msg);
		}

		private void VerifyAdminStatus(NetMessage msg)
		{
			if (HasPermission(TAG.VV_EDIT) == false) return;

			if (Librarian.IDToBookShelf.TryGetValue(msg.VVObjectID, out var shelf))
			{
				if (shelf.Shelf != null)
				{
					NetworkIdentity NetID = null;
					var Handler= shelf.Shelf.GetComponent<SpriteHandler>();
					if (Handler != null && Handler.NetworkThis)
					{
						NetID = SpriteHandlerManager.GetRecursivelyANetworkBehaviour(shelf.Shelf);
						SpriteHandlerManager.UnRegisterHandler(NetID, Handler);
					}

					var oldName = shelf.Shelf.name;

					shelf.Shelf.name = msg.NewName;

					if (Handler != null && Handler.NetworkThis)
					{
						SpriteHandlerManager.RegisterHandler(NetID, Handler);
					}


					if (msg.NetworkToClients)
					{
						UpdateClientValue.Send( msg.NewName, oldName,
							"",
							shelf.Shelf, UpdateClientValue.Modifying.RenamingGameObject);
					}

				}
			}


		}

		public static NetMessage Send( ulong VVObjectID ,string NewName, bool SendToToClient )
		{
			NetMessage msg = new NetMessage()
			{
				VVObjectID = VVObjectID,
				NewName = NewName,
				NetworkToClients = SendToToClient
			};

			Send(msg);
			return msg;
		}
	}
}