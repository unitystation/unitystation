using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using CameraEffects;

public class PlayerEatDrinkEffects : NetworkBehaviour
{
	[Server]
	public void ServerSendMessageToClient(GameObject client, int newValue)
	{
		PlayerEatDrinkEffectsServerMessage.Send(client, newValue);
	}
}

public class PlayerEatDrinkEffectsServerMessage : ServerMessage
{
	public int alcoholValue;

	public override void Process()
	{
		var camera = Camera.main;
		if (camera == null) return;
		camera.GetComponent<CameraEffectControlScript>().drunkCameraTime += alcoholValue;
	}

	/// <summary>
	/// Send full update to a client
	/// </summary>
	public static PlayerEatDrinkEffectsServerMessage Send(GameObject clientConn, int newAlcoholValue)
	{
		PlayerEatDrinkEffectsServerMessage msg = new PlayerEatDrinkEffectsServerMessage
		{
			alcoholValue = newAlcoholValue
		};
		msg.SendTo(clientConn);
		return msg;
	}
}
