using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	/// <summary>
	///     Request hacking node data from the server.
	/// </summary>
	public class RequestHackingNodeConnections : ClientMessage<RequestHackingNodeConnections.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
			public uint HackableObject;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] { msg.Player, msg.HackableObject });

			var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
			var hackObject = NetworkObjects[1];
			if (playerScript.IsGameObjectReachable(hackObject, true, context: hackObject))
			{

				HackingProcessBase hackProcess = hackObject.GetComponent<HackingProcessBase>();
				List<int[]> connectionList = hackProcess.GetNodeConnectionList();
				if (connectionList != null)
				{
					SendDataToClient(NetworkObjects[0], hackObject, connectionList);
					return;
				}
			}
		}

		void SendDataToClient(GameObject recipient, GameObject hackObject, List<int[]> connectionList)
		{
			HackingNodeConnectionList.Send(recipient, hackObject, connectionList);
		}

		public static NetMessage Send(GameObject player, GameObject hackObject)
		{
			NetMessage msg = new NetMessage
			{
				Player = player.GetComponent<NetworkIdentity>().netId,
				HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			};

			Send(msg);
			return msg;
		}
	}
}
