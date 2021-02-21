using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;
using Newtonsoft.Json;

public class RemoveHackingConnection : ClientMessage
{
	public struct RemoveHackingConnectionNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
		public string JsonData;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RemoveHackingConnectionNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RemoveHackingConnectionNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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
