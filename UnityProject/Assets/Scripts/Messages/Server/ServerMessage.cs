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

//			only send to players that are currently controlled by a client
		if (PlayerList.Instance.connectedPlayers.ContainsValue(recipient)) {
			netIdentity.connectionToClient.Send(GetMessageType(), this);
		} 
//		else {
//			Debug.Log($"Not sending message {ToString()} to {recipient}");
//		}

		//Obsolete version:
		//NetworkServer.SendToClientOfPlayer(recipient, GetMessageType(), this);
		//Debug.LogFormat("SentTo {0}", this);
	}
}
