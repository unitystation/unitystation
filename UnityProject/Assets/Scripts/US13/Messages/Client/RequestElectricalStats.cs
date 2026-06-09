using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using US13.Messages.Server;
using US13.Player;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Systems.Electricity.Inheritance;

namespace US13.Messages.Client
{
	/// <summary>
	///     Request electrical stats from the server
	/// </summary>
	public class RequestElectricalStats : ClientMessage<RequestElectricalStats.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
			public uint ElectricalItem;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] {msg.Player, msg.ElectricalItem});

			var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
			if (playerScript.IsGameObjectReachable(NetworkObjects[1], true, context: NetworkObjects[1]))
			{
				//Try powered device first:
				var poweredDevice = NetworkObjects[1].GetComponent<ElectricalOIinheritance>();
				if (poweredDevice != null)
				{
					SendDataToClient(poweredDevice.InData.Data, NetworkObjects[0]);
					return;
				}
			}
		}

		void SendDataToClient(ElectronicData data, GameObject recipient)
		{
			string json = JsonConvert.SerializeObject(data);
			ElectricalStatsMessage.Send(recipient, json);
		}

		public static NetMessage Send(GameObject player, GameObject electricalItem)
		{
			NetMessage msg = new NetMessage
			{
				Player = player.GetComponent<NetworkIdentity>().netId,
				ElectricalItem = electricalItem.GetComponent<NetworkIdentity>().netId,
			};

			Send(msg);
			return msg;
		}
	}
}
