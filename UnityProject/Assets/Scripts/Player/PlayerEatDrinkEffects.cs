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
	public class PlayerEatDrinkEffectsServerMessageNetMessage : NetworkMessage
	{
		public int alcoholValue;
		public GameObject clientPlayer = null;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PlayerEatDrinkEffectsServerMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		var camera = Camera.main;
		if (camera == null) return;
		camera.GetComponent<CameraEffectControlScript>().AddDrunkTime(newMsg.alcoholValue);
	}

	/// <summary>
	/// Send full update to a client
	/// </summary>
	public static PlayerEatDrinkEffectsServerMessageNetMessage Send(GameObject clientConn, int newAlcoholValue)
	{
		PlayerEatDrinkEffectsServerMessageNetMessage msg = new PlayerEatDrinkEffectsServerMessageNetMessage
		{
			alcoholValue = newAlcoholValue
		};
		new PlayerEatDrinkEffectsServerMessage().SendTo(clientConn, msg);
		return msg;
	}
}
