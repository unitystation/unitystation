using Mirror;
using UnityEngine;
using US13.Items;
using US13.Messages.Client.Admin;
using Util;

namespace US13.Messages.Client.DevicAattributeEditor
{
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
			if (HasPermission(TAG.MAP_TAG) == false) return;


			if (msg.ObjectID !=  NetId.Empty &&  msg.ObjectID != NetId.Invalid)
			{
				msg.ObjectID.NetIdToGameObject().GetComponent<Attributes>().SyncIsMapped(msg.IsMapped, msg.IsMapped);
			}

		}

	}
}
