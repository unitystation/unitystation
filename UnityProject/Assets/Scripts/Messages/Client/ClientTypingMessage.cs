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
	public TypingState state;

	public override void Process()
	{
		// server side logic
		if (SentByPlayer == ConnectedPlayer.Invalid)
			return;

		var playerScript = SentByPlayer.Script;
		if (!playerScript)
			return;

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
