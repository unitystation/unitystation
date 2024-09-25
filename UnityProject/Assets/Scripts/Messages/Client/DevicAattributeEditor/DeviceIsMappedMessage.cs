using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class DeviceIsMappedMessage : ClientMessage<DeviceIsMappedMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint ObjectID;
		public bool IsMapped;

	}

	public static void Send(GameObject Object, bool IsMapped)
	{
		NetMessage msg = new NetMessage
		{
			ObjectID = Object == null ? NetId.Empty : Object.NetId(),
			IsMapped = IsMapped,
		};
		Send(msg);
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;


		if (msg.ObjectID !=  NetId.Empty &&  msg.ObjectID != NetId.Invalid)
		{
			msg.ObjectID.NetIdToGameObject().GetComponent<Attributes>().SyncIsMapped(msg.IsMapped, msg.IsMapped);
		}

	}

}
