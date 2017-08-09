using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

public abstract class ClientMessage<T> : GameMessage<T>
{
	public NetworkInstanceId SentBy;

	public void Send()
	{
		SentBy = LocalPlayerId();
		CustomNetworkManager.Instance.client.Send(MessageType, this);
	}

	public void SendUnreliable()
	{
		SentBy = LocalPlayerId();
		CustomNetworkManager.Instance.client.SendUnreliable(MessageType, this);
	}

	private static NetworkInstanceId LocalPlayerId()
	{
		return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
	}
}
