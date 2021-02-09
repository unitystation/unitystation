using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class HackingNodeConnectionList : ServerMessage
{
	public string JsonData;
	public uint Recipient;
	public uint HackingObject;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] { Recipient, HackingObject });
		List<int[]> data = JsonConvert.DeserializeObject<List<int[]>>(JsonData);

		if (NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI != null)
		{
			NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI.UpdateConnectionList(data);
		}
	}

	public static HackingNodeConnectionList Send(GameObject recipient, GameObject hackingObject, List<int[]> connectionList)
	{
		HackingNodeConnectionList msg =
			new HackingNodeConnectionList { Recipient = recipient.GetComponent<NetworkIdentity>().netId, HackingObject = hackingObject.GetComponent<NetworkIdentity>().netId, JsonData = JsonConvert.SerializeObject(connectionList) };

		msg.SendToNearbyPlayers(hackingObject.transform.position);
		return msg;
	}
}
