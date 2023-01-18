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

		if (NetworkObject.TryGetComponent<Mind>(out var Mind))
		{
			PlayerManager.SetMind(Mind);
		}
		else
		{
			PlayerManager.SetMind(null);
		}
	}



	public static void SendTo(NetworkConnectionToClient conn, Mind mind)
	{
		uint netID = 0;

		if (mind == null)
		{
			netID = NetId.Empty;
		}
		else
		{
			netID = mind.netId;
		}


		var msg = new NetMessage
		{
			Mind = netID
		};

		SendTo(conn, msg);
	}
}
