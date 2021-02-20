using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;

public class AddHackingDevice : ClientMessage
{
	public class AddHackingDeviceNetMessage : ActualMessage
	{
		public uint Player;
		public uint HackableObject;
		public uint HackingDevice;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AddHackingDeviceNetMessage;
		if (newMsg == null) return;

		LoadMultipleObjects(new uint[] { newMsg.Player, newMsg.HackableObject, newMsg.HackingDevice });

		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var hackObject = NetworkObjects[1];
		HackingDevice hackDevice = NetworkObjects[2].GetComponent<HackingDevice>();
		HackingProcessBase hackingProcess = hackObject.GetComponent<HackingProcessBase>();
		if (hackingProcess.ServerPlayerCanAddDevice(playerScript, hackDevice))
		{
			hackingProcess.AddHackingDevice(hackDevice);
			hackingProcess.ServerStoreHackingDevice(hackDevice);
			HackingNodeConnectionList.Send(NetworkObjects[0], hackObject, hackingProcess.GetNodeConnectionList());
		}
	}

	public static AddHackingDeviceNetMessage Send(GameObject player, GameObject hackObject, GameObject hackingDevice)
	{
		AddHackingDeviceNetMessage msg = new AddHackingDeviceNetMessage
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			HackingDevice = hackingDevice.GetComponent<NetworkIdentity>().netId
		};
		new AddHackingDevice().Send(msg);
		return msg;
	}
}
