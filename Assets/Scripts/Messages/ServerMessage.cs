using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Represents a network message sent from the server to clients.
/// Sending a message will invoke the Process() method on the client.
/// </summary>
public abstract class ServerMessage<T> : GameMessage<T>
{
	public void SendToAll()
	{
		NetworkServer.SendToAll(MessageType, this);
	}
}
