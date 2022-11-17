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
		if (SentByPlayer?.Script.OrNull()?.mind == null) return;


		UIActionManager.Instance.UpdatePlayer(SentByPlayer.GameObject, SentByPlayer.Connection);
	}

	public static NetMessage Send()
	{
		NetMessage msg = new NetMessage();
		Send(msg);
		return msg;
	}
}
