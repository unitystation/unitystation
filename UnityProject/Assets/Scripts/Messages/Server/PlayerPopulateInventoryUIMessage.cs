using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class PlayerPopulateInventoryUIMessage : ServerMessage<PlayerPopulateInventoryUIMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint NetIDOfStorage;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.NetIDOfStorage);
			UIManager.Instance.UI_SlotManager.RemoveAll();
			NetworkObject.GetComponent<DynamicItemStorage>().ShowClientUI();
		}

		public static NetMessage Send(DynamicItemStorage DIM, GameObject ToWho)
		{
			NetMessage msg = new NetMessage
			{
				NetIDOfStorage = DIM.netId
			};

			SendTo(ToWho, msg);
			return msg;
		}
	}
}