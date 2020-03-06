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
		throw new System.NotImplementedException();
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
