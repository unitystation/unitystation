using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Messages.Client.DeviceLinkMessage;
using Mirror;
using Objects.Atmospherics;
using Shared.Systems.ObjectConnection;
using UnityEngine;

public class DeviceRotateMessage : ClientMessage<DeviceRotateMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public uint ToRotate;
		public OrientationEnum RotateTo;
		public OrientationEnum OriginalDirection;
	}

	public static void Send(GameObject ToRotate, OrientationEnum RotateTo, OrientationEnum OriginalDirection)
	{
		NetMessage msg = new NetMessage
		{
			ToRotate = ToRotate == null ? NetId.Empty : ToRotate.NetId(),
			RotateTo = RotateTo,
			OriginalDirection = OriginalDirection
		};
		Send(msg);
	}

	public override void Process(NetMessage msg)
	{
		if (IsFromAdmin() == false) return;
		var Master = msg.ToRotate.NetworkIdentity().GetComponent<Rotatable>();
		Master.FaceDirection(msg.RotateTo);
		if (Master.TryGetComponent<MonoPipe>(out var MonoPipe))
		{
			var Difference = (int) msg.RotateTo.ToPipeRotate() - (int) msg.OriginalDirection.ToPipeRotate();
			if (Difference < 0)
			{
				Difference += 4;
			}
			MonoPipe.RotatePipe((byte)Difference, false);
		}

		return;

	}
}