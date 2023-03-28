using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;
public class Activate3DMode : ServerMessage<Activate3DMode.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{

	}

	public override void Process(NetMessage msg)
	{
		if (CustomNetworkManager.IsServer)
		{
			return;
		}
		GameManager.Instance.PromptConvertTo3D();
	}

	public static void SendToEveryone()
	{
		SendToAll(new NetMessage());
	}

	public static void SendTo(NetworkConnectionToClient Player)
	{
		SendTo(Player, new NetMessage());
	}


}
