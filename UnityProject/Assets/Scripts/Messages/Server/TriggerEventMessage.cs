using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Message that allows the server to broadcast an event to the client
/// </summary>
public class TriggerEventMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.TriggerEvent;

	public EVENT EventType;

	public override IEnumerator Process()
	{
		yield return null;
		TriggerEvent();
	}

	/// Raise the specified event
	private void TriggerEvent()
	{
		EventManager.Broadcast(EventType);
	}
	///     Sends the message to the player
	public static TriggerEventMessage Send(GameObject recipient, EVENT eventType)
	{
		TriggerEventMessage msg = new TriggerEventMessage();
		msg.EventType = eventType;
		msg.SendTo(recipient);
		return msg;
	}
}