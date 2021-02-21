using System.Collections;
using Mirror;
using UnityEngine;

/// <summary>
/// Message that allows the server to broadcast an event to clients.
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public class TriggerEventMessageNetMessage : NetworkMessage
	{
		public EVENT EventType;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public TriggerEventMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsg = msg as TriggerEventMessageNetMessage;
		if(newMsg == null) return;

		EventManager.Broadcast(newMsg.EventType);
	}

	/// <summary>
	/// Send the event message to a specific player.
	/// </summary>
	public static TriggerEventMessageNetMessage SendTo(GameObject recipient, EVENT eventType)
	{
		var msg = CreateMessage(eventType);

		new TriggerEventMessage().SendTo(recipient, msg);
		return msg;
	}

	/// <summary>
	/// Send the event message to all players.
	/// </summary>
	public static TriggerEventMessageNetMessage SendToAll(EVENT eventType)
	{
		var msg = CreateMessage(eventType);

		new TriggerEventMessage().SendToAll(msg);
		return msg;
	}

	private static TriggerEventMessageNetMessage CreateMessage(EVENT eventType)
	{
		return new TriggerEventMessageNetMessage
		{
			EventType = eventType,
		};
	}
}
