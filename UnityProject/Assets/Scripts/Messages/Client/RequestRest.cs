using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Messages.Client.VariableViewer;
using Mirror;
using UnityEngine;

public class RequestRest : ClientMessage<RequestRest.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public bool LayDown;
	}


	public override void Process(NetMessage msg)
	{
		if (msg.LayDown)
		{
			if(SentByPlayer.Script.PlayerStateSettings.CanRest == false) return;

			SentByPlayer.Script.registerTile.ServerLayDown();
		}
		else
		{
			if (SentByPlayer.Script.playerMove.HasALeg == false)
			{
				Chat.AddExamineMsg(SentByPlayer.GameObject,"You try standing up stand up but you have no legs!");
				return;
			}
			SentByPlayer.Script.registerTile.ServerStandUp(true, 0.3f);
		}
	}

	public static NetMessage Send(bool inLayDown)
	{
		NetMessage msg = new NetMessage();
		msg.LayDown = inLayDown;
		Send(msg);
		return msg;
	}
}
