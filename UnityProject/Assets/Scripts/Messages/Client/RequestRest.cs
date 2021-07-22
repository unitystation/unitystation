using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Messages.Client.VariableViewer;
using Mirror;
using UnityEngine;

public class RequestRest : ClientMessage<RequestRest.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public bool LayDown;
	}


	public override void Process(NetMessage msg)
	{
		if (msg.LayDown)
		{
			SentByPlayer.Script.RegisterTile.ServerLayDown();
		}
		else
		{
			SentByPlayer.Script.RegisterTile.ServerStandUp(true, 0.3f);
		}
	}

	public static NetMessage Send(bool inLayDown)
	{
		NetMessage msg = new NetMessage();
		msg.LayDown = inLayDown;
		Send(msg);
		return msg;
	}
}
