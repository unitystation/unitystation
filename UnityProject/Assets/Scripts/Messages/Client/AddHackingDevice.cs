using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;

public class AddHackingDevice : ClientMessage
{
	public struct AddHackingDeviceNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
		public uint HackingDevice;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AddHackingDeviceNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AddHackingDeviceNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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
