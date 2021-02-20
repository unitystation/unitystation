using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;
using Mirror;

public class SpriteRequestCurrentStateMessage : ClientMessage
{
	public class SpriteRequestCurrentStateMessageNetMessage : ActualMessage
	{
		public uint SpriteHandlerManager;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as SpriteRequestCurrentStateMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.SpriteHandlerManager);
		if (SentByPlayer == ConnectedPlayer.Invalid)
			return;

		NetworkObject.GetComponent<SpriteHandlerManager>().UpdateNewPlayer(SentByPlayer.Connection);
	}

	public static SpriteRequestCurrentStateMessageNetMessage Send(uint spriteHandlerManager)
	{
		var msg = new SpriteRequestCurrentStateMessageNetMessage()
		{
			SpriteHandlerManager = spriteHandlerManager
		};
		new SpriteRequestCurrentStateMessage().Send(msg);
		return msg;
	}
}
