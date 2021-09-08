using System.Collections.Generic;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Server
{
	public class HackingNodeConnectionList : ServerMessage<HackingNodeConnectionList.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string JsonData;
			public uint Recipient;
			public uint HackingObject;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] { msg.Recipient, msg.HackingObject });
			List<int[]> data = JsonConvert.DeserializeObject<List<int[]>>(msg.JsonData);

			if (NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI != null)
			{
				//NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI.UpdateConnectionList(data);
			}
		}

		public static NetMessage Send(GameObject recipient, GameObject hackingObject, List<int[]> connectionList)
		{
			NetMessage msg =
				new NetMessage { Recipient = recipient.GetComponent<NetworkIdentity>().netId, HackingObject = hackingObject.GetComponent<NetworkIdentity>().netId, JsonData = JsonConvert.SerializeObject(connectionList) };

			SendToNearbyPlayers(hackingObject.transform.position, msg);
			return msg;
		}
	}
}
