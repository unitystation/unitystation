using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TypingState
{
	TYPING,
	STOP_TYPING
}

public class ClientTypingMessage : ClientMessage
{
	public static short MessageType = (short)MessageTypes.ClientTypingMessage;

	public TypingState state;

	public override IEnumerator Process()
	{
		if (SentByPlayer == ConnectedPlayer.Invalid)
			yield break;

		var playerScript = SentByPlayer.Script;
		if (!playerScript)
			yield break;

		// resend it to all nearby players
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
