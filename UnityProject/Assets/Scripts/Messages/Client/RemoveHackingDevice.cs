using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;

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
}
