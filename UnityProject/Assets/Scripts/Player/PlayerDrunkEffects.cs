using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;
using Messages.Server;

public class PlayerDrunkEffects : NetworkBehaviour
{
	[Server]
	public void ServerSendMessageToClient(GameObject client, float newValue)
	{
		PlayerDrunkServerMessage.Send(client, newValue);
	}
}

public class PlayerDrunkServerMessage : ServerMessage<PlayerDrunkServerMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public float alcoholValue;
	}

	public override void Process(NetMessage msg)
	{
		var camera = Camera.main;
		if (camera == null) return;
		camera.GetComponent<CameraEffectControlScript>().AddDrunkTime(msg.alcoholValue);
	}

	/// <summary>
	/// Send full update to a client
	/// </summary>
	public static NetMessage Send(GameObject clientConn, float newAlcoholValue)
	{
		NetMessage msg = new NetMessage
		{
			alcoholValue = newAlcoholValue
		};

		SendTo(clientConn, msg);
		return msg;
	}
}
