using System.Linq;
using Mirror;
using US13.Messages.Server.AdminTools;
using US13.Systems.Inventory;
using US13.UI.Systems.AdminTools.DevTools;
using Util;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestInventories : ClientMessage<AdminRequestInventories.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Object;
			public bool IncludeEmpties;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_GHOST_INVENTORY))
			{
				LoadNetworkObject(msg.Object);
				var storages = NetworkObject.GetComponents<ItemStorage>();

				var Inventorys = storages.SelectMany(x =>  InventoryViewerDynamicManager.TraverseInventories(
					x,
					msg.IncludeEmpties));

				AdminReturnInventories.Send(SentByPlayer , Inventorys);

			}
		}

		public static NetMessage Send(ItemStorage ItemStorage, bool IncludeEmpties)
		{
			var msg = new NetMessage
			{
				Object = ItemStorage.gameObject.NetId(),
				IncludeEmpties = IncludeEmpties
			};

			Send(msg);
			return msg;
		}
	}
}