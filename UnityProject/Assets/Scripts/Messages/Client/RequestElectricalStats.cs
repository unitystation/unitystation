using System.Collections;
using Messages.Client;
using UnityEngine;
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
		if (playerScript.IsGameObjectReachable(NetworkObjects[1], true, context: NetworkObjects[1]))
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
}
