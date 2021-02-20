using System.Collections;
using UnityEngine;

/// <summary>
/// Message that allows the server to broadcast an event to clients.
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public class TriggerEventMessageNetMessage : ActualMessage
	{
		public EVENT EventType;
	}

	public override void Process(ActualMessage msg)
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
