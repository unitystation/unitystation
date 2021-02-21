using System.Collections;
using UnityEngine;
using Mirror;
using Messages.Client;

public class RemoveHackingDevice : ClientMessage
{
	public struct RemoveHackingDeviceNetMessage : NetworkMessage
	{
		public uint Player;
		public uint HackableObject;
		public uint HackingDevice;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RemoveHackingDeviceNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RemoveHackingDeviceNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadMultipleObjects(new uint[] { newMsg.Player, newMsg.HackableObject, newMsg.HackingDevice });

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

	public static RemoveHackingDeviceNetMessage Send(GameObject player, GameObject hackObject, GameObject hackingDevice)
	{
		RemoveHackingDeviceNetMessage msg = new RemoveHackingDeviceNetMessage
		{
			Player = player.GetComponent<NetworkIdentity>().netId,
			HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
			HackingDevice = hackingDevice.GetComponent<NetworkIdentity>().netId
		};

		new RemoveHackingDevice().Send(msg);
		return msg;
	}
}
