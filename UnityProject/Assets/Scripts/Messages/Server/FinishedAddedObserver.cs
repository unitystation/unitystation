using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

public class FinishedAddedObserver : ServerMessage<FinishedAddedObserver.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string MapName;
	}


	public override void Process(NetMessage msg)
	{
		SubSceneManager.Instance.ClientObserver[msg.MapName] = true;
	}

	public static NetMessage Send(NetworkConnection recipient, string Map)
	{

		NetMessage  msg =
			new NetMessage  { MapName =  Map};

		SendTo(recipient, msg);
		return msg;
	}

}
