using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;
using System.Collections.Generic;
using Newtonsoft.Json;

public class RemoveHackingDevice: ClientMessage
{
	public uint Player;
	public uint HackableObject;
	public uint HackingDevice;

	public override void Process()
	{
		LoadMultipleObjects(new uint[] { Player, HackableObject, HackingDevice });

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		HackingDevice hackDevice = NetworkObjects[2].GetComponent<HackingDevice>();
		HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
		if (hackingProcess.ServerPlayerCanRemoveDevice(playerScript, hackDevice))
		{
			hackingProcess.RemoveHackingDevice(hackDevice);
			hackingProcess.ServerPlayerRemoveHackingDevice(playerScript, hackDevice);
			HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
		}
	}

	public static RemoveHackingDevice Send(GameObject player, GameObject hackObject, GameObject hackingDevice)
	{
		RemoveHackingDevice msg = new RemoveHackingDevice
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			HackingDevice = hackingDevice.GetComponent<NetworkIdentity>().netId
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Player = reader.ReadUInt32();
		HackableObject = reader.ReadUInt32();
		HackingDevice = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Player);
		writer.WriteUInt32(HackableObject);
		writer.WriteUInt32(HackingDevice);
	}
}
