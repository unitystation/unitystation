using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class SpriteRequestCurrentStateMessage : ClientMessage
{
	public uint SpriteHandlerManager;

	public override void Process()
	{
		LoadNetworkObject(SpriteHandlerManager);
		if (SentByPlayer == ConnectedPlayer.Invalid)
			return;

		NetworkObject.GetComponent<SpriteHandlerManager>().UpdateNewPlayer(SentByPlayer.Connection);
	}

	public static SpriteRequestCurrentStateMessage Send(uint spriteHandlerManager)
	{
		var msg = new SpriteRequestCurrentStateMessage()
		{
			SpriteHandlerManager = spriteHandlerManager
		};
		msg.Send();
		return msg;
	}
}
