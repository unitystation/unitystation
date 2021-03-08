using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public class AddHackingDevice : ClientMessage<AddHackingDevice.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Player;
			public uint HackableObject;
			public uint HackingDevice;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[] { msg.Player, msg.HackableObject, msg.HackingDevice });

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

		public static NetMessage Send(GameObject player, GameObject hackObject, GameObject hackingDevice)
		{
			NetMessage msg = new NetMessage
			{
				Player = player.GetComponent<NetworkIdentity>().netId,
				HackableObject = hackObject.GetComponent<NetworkIdentity>().netId,
				HackingDevice = hackingDevice.GetComponent<NetworkIdentity>().netId
			};

			Send(msg);
			return msg;
		}
	}
}
