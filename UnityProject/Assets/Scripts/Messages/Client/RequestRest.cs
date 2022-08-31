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
		if (SentByPlayer.Script.registerTile.IsLayingDown == false && SentByPlayer.Script.PlayerTypeSettings.CanRest == true)
		{
			SentByPlayer.Script.registerTile.ServerLayDown();
		}
		else
		{
			if(msg.LayDown == true) //If a rest request is trying to force knock us down, we dont want it to toggle a stand up if we are already resting.
			{
				if (SentByPlayer.Script.playerMove.HasALeg == false)
				{
					Chat.AddExamineMsg(SentByPlayer.GameObject, "You try standing up stand up but you have no legs!");
				}
				return;
			}
			SentByPlayer.Script.registerTile.ServerStandUp(true, 0.3f);
		}
	}

	public static NetMessage Send(bool layDown = false)
	{
		NetMessage msg = new NetMessage();
		msg.LayDown = layDown;
		Send(msg);
		return msg;
	}
}
