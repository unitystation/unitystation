using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;
using Messages.Server;

public class PlayerEatDrinkEffects : NetworkBehaviour
{
	[Server]
	public void ServerSendMessageToClient(GameObject client, int newValue)
	{
		PlayerEatDrinkEffectsServerMessage.Send(client, newValue);
	}
}

public class PlayerEatDrinkEffectsServerMessage : ServerMessage<PlayerEatDrinkEffectsServerMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public int alcoholValue;
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
	public static NetMessage Send(GameObject clientConn, int newAlcoholValue)
	{
		NetMessage msg = new NetMessage
		{
			alcoholValue = newAlcoholValue
		};

		SendTo(clientConn, msg);
		return msg;
	}
}
