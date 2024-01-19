using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Messages.Client.DeviceLinkMessage;
using Mirror;
using Shared.Systems.ObjectConnection;
using UnityEngine;

public class DeviceRotateMessage : ClientMessage<DeviceRotateMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint ToRotate;
		public OrientationEnum RotateTo;
	}

	public static void Send(GameObject ToRotate, OrientationEnum RotateTo)
	{
		NetMessage msg = new NetMessage
		{
			ToRotate = ToRotate == null ? NetId.Empty : ToRotate.NetId(),
			RotateTo = RotateTo
		};
		Send(msg);
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;
		var Master = msg.ToRotate.NetworkIdentity().GetComponent<Rotatable>();
		Master.FaceDirection(msg.RotateTo);
		return;

	}
}