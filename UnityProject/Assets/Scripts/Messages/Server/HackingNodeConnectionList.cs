using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Newtonsoft.Json;

public class HackingNodeConnectionList : ServerMessage
{
	public class HackingNodeConnectionListNetMessage : NetworkMessage
	{
		public string JsonData;
		public uint Recipient;
		public uint HackingObject;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as HackingNodeConnectionListNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadMultipleObjects(new uint[] { newMsg.Recipient, newMsg.HackingObject });
		List<int[]> data = JsonConvert.DeserializeObject<List<int[]>>(newMsg.JsonData);

		if (NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI != null)
		{
			NetworkObjects[1].GetComponent<HackingProcessBase>().HackingGUI.UpdateConnectionList(data);
		}
	}

	public static HackingNodeConnectionListNetMessage Send(GameObject recipient, GameObject hackingObject, List<int[]> connectionList)
	{
		HackingNodeConnectionListNetMessage msg =
			new HackingNodeConnectionListNetMessage { Recipient = recipient.GetComponent<NetworkIdentity>().netId, HackingObject = hackingObject.GetComponent<NetworkIdentity>().netId, JsonData = JsonConvert.SerializeObject(connectionList) };

		new HackingNodeConnectionList().SendToNearbyPlayers(hackingObject.transform.position, msg);
		return msg;
	}
}
