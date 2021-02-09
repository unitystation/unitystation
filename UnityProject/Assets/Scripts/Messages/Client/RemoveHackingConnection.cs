using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class RemoveHackingConnection : ClientMessage
{
	public uint Player;
	public uint HackableObject;
	public string JsonData;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] { Player, HackableObject });
		int[] connectionToRemove = JsonConvert.DeserializeObject<int[]>(JsonData);

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
		if (hackingProcess.ServerPlayerCanRemoveConnection(playerScript, connectionToRemove))
		{
			hackingProcess.ServerPlayerRemoveConnection(playerScript, connectionToRemove);
			HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
		}
	}

	public static RemoveHackingConnection Send(GameObject player, GameObject hackObject, int[] connectionToRemove)
	{
		RemoveHackingConnection msg = new RemoveHackingConnection
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			JsonData = JsonConvert.SerializeObject(connectionToRemove),
		};
		msg.Send();
		return msg;
	}
}
