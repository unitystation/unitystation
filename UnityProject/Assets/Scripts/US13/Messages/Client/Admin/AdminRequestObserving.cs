using System;
using Mirror;
using US13.Systems.Inventory;
using Util;

namespace US13.Messages.Client.Admin
{
	public class AdminRequestObserving : ClientMessage<AdminRequestObserving.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ItemStorage;
			public int Index;
			public bool StopObserving;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_GHOST_INVENTORY))
			{
				LoadNetworkObject(msg.ItemStorage);
				var Storage = NetworkObject.GetComponents<ItemStorage>()[msg.Index];
				if (msg.StopObserving == false)
				{
					Storage.ServerAddObserverPlayer(SentByPlayer.GameObject, true);
				}
				else
				{
					Storage.ServerRemoveObserverPlayer(SentByPlayer.GameObject, true);
				}

			}
		}

		public static NetMessage Send(ItemStorage ItemStorage, bool Stop)
		{

			if (ItemStorage == null) return new NetMessage();
			var msg = new NetMessage
			{
				StopObserving = Stop,
				ItemStorage = ItemStorage.gameObject.NetId(),
				Index = Array.IndexOf(ItemStorage.gameObject.GetComponents<ItemStorage>(), ItemStorage)
			};

			Send(msg);
			return msg;
		}
	}
}
