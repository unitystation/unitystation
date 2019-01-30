using System.Collections;
using UnityEngine.Networking;

public abstract class ClientMessage : GameMessageBase
{
/// <summary>
/// Player that sent this ClientMessage.
/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
/// </summary>
	public ConnectedPlayer SentByPlayer;
	public override IEnumerator Process( NetworkConnection sentBy )
	{
		SentByPlayer = PlayerList.Instance.Get( sentBy );
		return base.Process( sentBy );
	}

	public void Send()
	{
		CustomNetworkManager.Instance.client.connection.Send(GetMessageType(), this);
//		Logger.Log($"Sent {this}");
	}

	public void SendUnreliable()
	{
		CustomNetworkManager.Instance.client.connection.SendUnreliable(GetMessageType(), this);
	}

	private static NetworkInstanceId LocalPlayerId()
	{

		return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
	}
}
