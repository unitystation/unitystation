using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;

namespace Messages.Client
{
	public class RemoveHackingConnection : ClientMessage<RemoveHackingConnection.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
			public uint HackableObject;
			public string JsonData;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] { msg.Player, msg.HackableObject });
			int[] connectionToRemove = JsonConvert.DeserializeObject<int[]>(msg.JsonData);

			var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
			var hackObject = NetworkObjects[1];
			HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
			if (hackingProcess.ServerPlayerCanRemoveConnection(playerScript, connectionToRemove))
			{
				hackingProcess.ServerPlayerRemoveConnection(playerScript, connectionToRemove);
				HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
			}
		}

		public static NetMessage Send(GameObject player, GameObject hackObject, int[] connectionToRemove)
		{
			NetMessage msg = new NetMessage
			{
				Player = player.GetComponent<NetworkIdentity>().netId,
				HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
				JsonData = JsonConvert.SerializeObject(connectionToRemove),
			};

			Send(msg);
			return msg;
		}
	}
}
