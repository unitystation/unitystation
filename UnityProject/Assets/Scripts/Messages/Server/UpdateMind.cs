using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

public class UpdateMind : ServerMessage<UpdateMind.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint Mind;
	}
	public override void Process(NetMessage msg)
	{
		LoadNetworkObject(msg.Mind);
		if (NetworkObject == null)
		{
			PlayerManager.SetMind(null);
		}

		PlayerManager.SetMind(NetworkObject.GetComponent<Mind>());
	}



	public static void SendTo(NetworkConnectionToClient conn, Mind mind)
	{
		uint netID = 0;

		netID = mind == null ? NetId.Empty : mind.netId;

		var msg = new NetMessage
		{
			Mind = netID
		};

		SendTo(conn, msg);
	}
}
