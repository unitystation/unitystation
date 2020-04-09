using System.Collections;
using Mirror;

public abstract class ClientMessage : GameMessageBase
{
/// <summary>
/// Player that sent this ClientMessage.
/// Returns ConnectedPlayer.Invalid if there are issues finding one from PlayerList (like, player already left)
/// </summary>
	public ConnectedPlayer SentByPlayer;
	public override void Process( NetworkConnection sentBy )
	{
		SentByPlayer = PlayerList.Instance.Get( sentBy );
		base.Process(sentBy);
	}

	public void Send()
	{
		NetworkClient.Send(this, 0);
//		Logger.Log($"Sent {this}");
	}

	public void SendUnreliable()
	{
		NetworkClient.Send(this, 1);
	}

	private static uint LocalPlayerId()
	{

		return PlayerManager.LocalPlayer.GetComponent<NetworkIdentity>().netId;
	}
}
