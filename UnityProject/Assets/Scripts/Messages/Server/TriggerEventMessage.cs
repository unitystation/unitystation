using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message that allows the server to broadcast an event to the client
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public EVENT EventType;

	public override void Process()
	{
		TriggerEvent();
	}

	/// Raise the specified event
	private void TriggerEvent()
	{
		EventManager.Broadcast(EventType);
	}
	/// <summary>
	/// Sends the event message to the player.
	/// </summary>
	/// <param name="recipient"></param>
	/// <param name="eventType"></param>
	/// <returns></returns>
	public static TriggerEventMessage Send(GameObject recipient, EVENT eventType)
	{
		TriggerEventMessage msg = new TriggerEventMessage();
		msg.EventType = eventType;
		msg.SendTo(recipient);
		return msg;
	}
}