using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Recipient = reader.ReadUInt32();
		HackingObject = reader.ReadUInt32();
		JsonData = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Recipient);
		writer.WriteUInt32(HackingObject);
		writer.WriteString(JsonData);
	}
}
