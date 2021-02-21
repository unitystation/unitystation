using System.Collections;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using Messages.Client;

/// <summary>
///     Request hacking node data from the server.
/// </summary>
public class RequestHackingNodeConnections : ClientMessage
{
	public class RequestHackingNodeConnectionsNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as RequestHackingNodeConnectionsNetMessage;
		if(newMsg == null) return;

		LoadMultipleObjects(new uint[] { newMsg.Player, newMsg.HackableObject });

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

	public static RequestHackingNodeConnectionsNetMessage Send(GameObject player, GameObject hackObject)
	{
		RequestHackingNodeConnectionsNetMessage msg = new RequestHackingNodeConnectionsNetMessage
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
		};
		new RequestHackingNodeConnections().Send(msg);
		return msg;
	}
}
