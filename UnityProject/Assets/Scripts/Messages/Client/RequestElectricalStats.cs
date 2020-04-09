﻿using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

/// <summary>
///     Request electrical stats from the server
/// </summary>
public class RequestElectricalStats : ClientMessage
{
	public uint Player;
	public uint ElectricalItem;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] {Player, ElectricalItem});

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		if (playerScript.IsInReach(NetworkObjects[1], true))
		{
			//Try powered device first:
			var poweredDevice = NetworkObjects[1].GetComponent<ElectricalOIinheritance>();
			if (poweredDevice != null)
			{
				SendDataToClient(poweredDevice.InData.Data, NetworkObjects[0]);
				return;
			}
		}
	}

	void SendDataToClient(ElectronicData data, GameObject recipient)
	{
		string json = JsonUtility.ToJson(data);
		ElectricalStatsMessage.Send(recipient, json);
	}

	public static RequestElectricalStats Send(GameObject player, GameObject electricalItem)
	{
		RequestElectricalStats msg = new RequestElectricalStats
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
				ElectricalItem = electricalItem.GetComponent<NetworkIdentity>().netId,
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Player = reader.ReadUInt32();
		ElectricalItem = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Player);
		writer.WriteUInt32(ElectricalItem);
	}
}