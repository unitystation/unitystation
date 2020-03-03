﻿using System.Collections;

/// <summary>
///     A way to broadcast messages on the client via the server
/// </summary>
public class ServerToClientEventsMsg : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ServerToClientEvents;
	public EVENT Event;

	public override IEnumerator Process()
	{
		yield return null;

		EventManager.Broadcast(Event);
	}
	
	/// <summary>
	/// Send an event from EventManager to all clients
	/// </summary>
	public static ServerToClientEventsMsg SendToAll(EVENT _event)
	{
		ServerToClientEventsMsg msg = new ServerToClientEventsMsg {Event = _event};

		msg.SendToAll();

		return msg;
	}
}