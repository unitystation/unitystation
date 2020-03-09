using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TypingState
{
	TYPING,
	STOP_TYPING
}

/// <summary>
/// Messsage from client to server that indicate that local player starts/stops typing
/// </summary>
public class ClientTypingMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.ClientTypingMessage;

	public TypingState state;

	public override IEnumerator Process()
	{
		// server side logic
		if (SentByPlayer == ConnectedPlayer.Invalid)
			yield break;

		var playerScript = SentByPlayer.Script;
		if (!playerScript)
			yield break;

		// send it to server that will decide what should be done next
		ServerTypingMessage.Send(playerScript, state);
	}

	public static ClientTypingMessage Send(TypingState newState)
	{
		var msg = new ClientTypingMessage()
		{
			state = newState
		};
		msg.Send();
		return msg;
	}
}
