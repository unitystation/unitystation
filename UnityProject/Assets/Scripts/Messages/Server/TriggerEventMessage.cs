using System.Collections;
using UnityEngine;

/// <summary>
/// Message that allows the server to broadcast an event to clients.
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public EVENT EventType;

	public override void Process()
	{
		EventManager.Broadcast(EventType);
	}

	/// <summary>
	/// Send the event message to a specific player.
	/// </summary>
	public static TriggerEventMessage SendTo(GameObject recipient, EVENT eventType)
	{
		var msg = CreateMessage(eventType);

		msg.SendTo(recipient);
		return msg;
	}

	/// <summary>
	/// Send the event message to all players.
	/// </summary>
	public static TriggerEventMessage SendToAll(EVENT eventType)
	{
		var msg = CreateMessage(eventType);

		msg.SendToAll();
		return msg;
	}

	private static TriggerEventMessage CreateMessage(EVENT eventType)
	{
		return new TriggerEventMessage
		{
			EventType = eventType,
		};
	}
}
