using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Represents a network message sent from the server to clients.
///     Sending a message will invoke the Process() method on the client.
/// </summary>
public abstract class ServerMessage : GameMessageBase
{
	public void SendToAll()
	{
		NetworkServer.SendToAll(GetMessageType(), this);
		//		Debug.LogFormat("SentToAll {0}", this);
	}

	public void SendTo(GameObject recipient)
	{
		NetworkIdentity netIdentity = recipient.GetComponent<NetworkIdentity>();

		//Only send to the client of the currently owned player as some
		//netID's being used in this method could be dead players, exclude them:
		if (PlayerList.Instance.connectedPlayers.ContainsValue(recipient)) {
			netIdentity.connectionToClient.Send(GetMessageType(), this);
		} else {
			//only send to players that are currently controlled by a client
			return;
		}

		//Obsolete version:
		//NetworkServer.SendToClientOfPlayer(recipient, GetMessageType(), this);
		//Debug.LogFormat("SentTo {0}", this);
	}
}
