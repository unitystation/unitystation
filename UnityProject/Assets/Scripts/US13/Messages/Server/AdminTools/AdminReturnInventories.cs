using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using US13.Managers;
using US13.Managers.NetworkManagement;
using US13.Systems.Inventory;
using US13.UI.Systems.AdminTools.DevTools;
using Util;

namespace US13.Messages.Server.AdminTools
{
	public class AdminReturnInventories : ServerMessage<AdminReturnInventories.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public IndexAndUint[] Storages;


			public struct IndexAndUint
			{
				public int Index;
				public uint Uint;
			}

		}

		public override void Process(NetMessage msg)
		{
			var List = new List<ItemStorage>();
			foreach (var Store in msg.Storages)
			{
				if (CustomNetworkManager.Spawned.ContainsKey(Store.Uint))
				{
					List.Add( CustomNetworkManager.Spawned[Store.Uint].GetComponents<ItemStorage>()[Store.Index]);
				}
			}
			InventoryViewerDynamicManager.Instance.LoadInventories(List);
		}




		public static NetMessage Send(PlayerInfo player, IEnumerable<ItemStorage> ItemStorages)
		{
			NetMessage msg = new NetMessage
			{
				Storages = ItemStorages.Select(x => new NetMessage.IndexAndUint()
				{
					Uint = x.gameObject.NetId(),
					Index = Array.IndexOf(x.gameObject.GetComponents<ItemStorage>(), x)
				} ).ToArray()
			};

			SendTo(player.Connection, msg);
			return msg;
		}
	}
}
