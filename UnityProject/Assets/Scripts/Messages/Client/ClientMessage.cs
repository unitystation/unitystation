using System.Collections;
using Mirror;

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
		NetworkClient.Send(MessageType, this);
//		Logger.Log($"Sent {this}");
	}

	public void SendUnreliable()
	{
		NetworkClient.Send(MessageType, this);
	}

	private static uint LocalPlayerId()
	{

		return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
	}
}
