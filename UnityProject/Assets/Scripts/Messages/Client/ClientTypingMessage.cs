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

	public TypingState TypingState;

	public override IEnumerator Process()
	{
		if (SentByPlayer == ConnectedPlayer.Invalid)
			yield break;

		var playerScript = SentByPlayer.Script;
		if (!playerScript)
			yield break;

		var typingIcon = playerScript.chatIcon;
	}

	public static ClientTypingMessage Send(TypingState state)
	{
		var msg = new ClientTypingMessage()
		{
			TypingState = state
		};
		msg.Send();
		return msg;
	}
}
