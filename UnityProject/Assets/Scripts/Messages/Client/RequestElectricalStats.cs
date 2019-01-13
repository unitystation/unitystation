using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Request electrical stats from the server
/// </summary>
public class RequestElectricalStats : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestElectricalStats;

	public NetworkInstanceId Player;
	public NetworkInstanceId ElectricalItem;

	public override IEnumerator Process()
	{
		yield return WaitFor(Player, ElectricalItem);
		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		if (playerScript.IsInReach(NetworkObjects[1]))
		{
			//Try powered device first:
			var poweredDevice = NetworkObjects[1].GetComponent<PoweredDevice>();
			if (poweredDevice != null)
			{
				SendDataToClient(poweredDevice.Data, NetworkObjects[0]);
				yield break;
			}

			//Try power supplies:
			var powerSupply = NetworkObjects[1].GetComponent<PowerSupply>();
			if (powerSupply != null)
			{
				SendDataToClient(powerSupply.Data, NetworkObjects[0]);
				yield break;
			}

			//Try Inline device next:
			var inlineDevice = NetworkObjects[1].GetComponent<InLineDevice>();
			if (inlineDevice != null)
			{
				SendDataToClient(inlineDevice.Data, NetworkObjects[0]);
				yield break;
			}

			//Last of all check if it is just a wire
			var wireConnect = NetworkObjects[1].GetComponent<InLineDevice>();
			if (wireConnect != null)
			{
				SendDataToClient(wireConnect.Data, NetworkObjects[0]);
				yield break;
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
		Player = reader.ReadNetworkId();
		ElectricalItem = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Player);
		writer.Write(ElectricalItem);
	}
}