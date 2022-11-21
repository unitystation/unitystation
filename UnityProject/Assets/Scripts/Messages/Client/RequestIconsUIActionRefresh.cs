using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UI.Action;
using UnityEngine;

public class RequestIconsUIActionRefresh : ClientMessage<RequestIconsUIActionRefresh.NetMessage>
{
	public struct NetMessage : NetworkMessage { }

	public override void Process(NetMessage msg)
	{
		if (SentByPlayer.Mind == null) return;

		var Bodies = SentByPlayer.Mind.GetRelatedBodies();
		foreach (var Body in Bodies)
		{
			UIActionManager.Instance.UpdatePlayer(Body.gameObject, SentByPlayer.Connection);
		}


	}

	public static NetMessage Send()
	{
		NetMessage msg = new NetMessage();
		Send(msg);
		return msg;
	}
}
