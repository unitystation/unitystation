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
		Logger.LogTraceFormat("SentToAll {0}", Category.NetMessage, this);
	}

	public void SendTo(GameObject recipient)
	{
		if ( recipient == null ) {
			return;
		}
		NetworkConnection connection = recipient.GetComponent<NetworkIdentity>().connectionToClient;
//			only send to players that are currently controlled by a client
		if (PlayerList.Instance.ContainsConnection(connection)) {
			connection.Send(GetMessageType(), this);
			Logger.LogTraceFormat( "SentTo {0}: {1}", Category.NetMessage, recipient.name, this );
		} else {
			Logger.LogTraceFormat( "Not sending message {0} to {1}", Category.NetMessage, this, recipient.name );
		}

		//Obsolete version:
		//NetworkServer.SendToClientOfPlayer(recipient, GetMessageType(), this);
	}
}
