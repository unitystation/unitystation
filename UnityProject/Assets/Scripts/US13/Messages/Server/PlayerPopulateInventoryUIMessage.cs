using Mirror;
using UnityEngine;
using US13.Systems.Inventory;
using US13.UI.Systems;

namespace US13.Messages.Server
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
			UIManager.Instance.UI_SlotManager.UpdateUI();
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