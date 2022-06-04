using System.Collections;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
using UnityEngine;

namespace Messages.Client
{
	public enum TypingState
	{
		TYPING,
		STOP_TYPING
	}

	/// <summary>
	/// Messsage from client to server that indicate that local player starts/stops typing
	/// </summary>
	public class ClientTypingMessage : ClientMessage<ClientTypingMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public TypingState state;
		}

		public override void Process(NetMessage msg)
		{
			// server side logic
			if (SentByPlayer == PlayerInfo.Invalid)
				return;

			var playerScript = SentByPlayer.Script;
			if (!playerScript)
				return;

			// send it to server that will decide what should be done next
			ServerTypingMessage.Send(playerScript, msg.state);
		}

		public static NetMessage Send(TypingState newState)
		{
			var msg = new NetMessage()
			{
				state = newState
			};

			Send(msg);
			return msg;
		}
	}
}