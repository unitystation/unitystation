using Mirror;
using UnityEngine;
using US13.Core.Transform;
using US13.Messages.Client.Admin;
using US13.Objects.Directionals;
using Util;

namespace US13.Messages.Client.DeviceRotator
{
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
			if (HasPermission(TAG.MAP_ROTATE) == false) return;
			var Master = msg.ToRotate.NetworkIdentity().GetComponent<Rotatable>();
			Master.FaceDirection(msg.RotateTo);
		}
	}
}