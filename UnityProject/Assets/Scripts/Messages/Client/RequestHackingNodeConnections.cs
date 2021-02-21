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
	public struct RequestHackingNodeConnectionsNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestHackingNodeConnectionsNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestHackingNodeConnectionsNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
