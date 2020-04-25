using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using System.Collections.Generic;

/// <summary>
///     Request hacking node data from the server.
/// </summary>
public class RequestHackingNodeConnections : ClientMessage
{
	public uint Player;
	public uint HackableObject;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] { Player, HackableObject });

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		if (playerScript.IsInReach(hackObject, true))
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

	public static RequestHackingNodeConnections Send(GameObject player, GameObject hackObject)
	{
		RequestHackingNodeConnections msg = new RequestHackingNodeConnections
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Player = reader.ReadUInt32();
		HackableObject = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Player);
		writer.WriteUInt32(HackableObject);
	}
}