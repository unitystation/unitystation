using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;


public class HackingNodeConnectionList : ServerMessage
{
	public string JsonData;
	public uint Recipient;//fixme: Recipient is redundant! Can be safely removed

	public override void Process()
	{
		LoadNetworkObject(Recipient);
		List<int[]> data = JsonUtility.FromJson<List<int[]>>(JsonData);

		NetworkObject.GetComponent<HackingProcessBase>().HackingGUI.UpdateConnectionList(data);
	}

	public static HackingNodeConnectionList Send(GameObject recipient, List<int[]> connectionList)
	{
		HackingNodeConnectionList msg =
			new HackingNodeConnectionList { Recipient = recipient.GetComponent<NetworkIdentity>().netId, JsonData = JsonUtility.ToJson(connectionList) };

		msg.SendTo(recipient);
		return msg;
	}
}
