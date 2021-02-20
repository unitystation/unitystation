using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class RemoveHackingConnection : ClientMessage
{
	public class RemoveHackingConnectionNetMessage : ActualMessage
	{
		public uint Player;
		public uint HackableObject;
		public string JsonData;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as RemoveHackingConnectionNetMessage;
		if(newMsg == null) return;

		LoadMultipleObjects(new uint[] { newMsg.Player, newMsg.HackableObject });
		int[] connectionToRemove = JsonConvert.DeserializeObject<int[]>(newMsg.JsonData);

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
		if (hackingProcess.ServerPlayerCanRemoveConnection(playerScript, connectionToRemove))
		{
			hackingProcess.ServerPlayerRemoveConnection(playerScript, connectionToRemove);
			HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
		}
	}

	public static RemoveHackingConnectionNetMessage Send(GameObject player, GameObject hackObject, int[] connectionToRemove)
	{
		RemoveHackingConnectionNetMessage msg = new RemoveHackingConnectionNetMessage
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			JsonData = JsonConvert.SerializeObject(connectionToRemove),
		};

		new RemoveHackingConnection().Send(msg);
		return msg;
	}
}
