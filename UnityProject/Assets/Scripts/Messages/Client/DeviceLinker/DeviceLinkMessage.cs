using System.Collections;
using System.Collections.Generic;
using Initialisation;
using Mirror;
using Shared.Systems.ObjectConnection;
using UnityEngine;

namespace Messages.Client.DeviceLinkMessage
{
	public class DeviceLinkMessage : ClientMessage<DeviceLinkMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Master;
			public uint Slave;
			public MultitoolConnectionType MultitoolConnectionType;

		}

		public static void Send(GameObject SetMaster , MultitoolConnectionType ConType, GameObject Slave)
		{
			NetMessage msg = new NetMessage
			{
				Master = SetMaster == null ? NetId.Empty : SetMaster.NetId(),
				Slave = Slave == null ? NetId.Empty : Slave.NetId(),
				MultitoolConnectionType = ConType
			};
			Send(msg);

		}

		public override void Process(NetMessage msg)
		{
			if (IsFromAdmin() == false) return;

			if (msg.Slave is NetId.Empty or NetId.Invalid)
			{
				var Master = msg.Master.NetworkIdentity().GetComponent<IMultitoolMasterable>();
				InGameDeviceLinker.Instance.Refresh();
				var Object = SentByPlayer.GameObject;
				DeviceUpdateLinkerInfoMessage.SendTo(Object, InGameDeviceLinker.Instance.MastersData[msg.MultitoolConnectionType][Master], Master.gameObject);
				return;
			}

			foreach (var ImultitoolSlaveable in msg.Slave.NetworkIdentity().GetComponents<IMultitoolSlaveable>())
			{
				if (ImultitoolSlaveable.ConType == msg.MultitoolConnectionType)
				{
					var MasterObject = msg.Master.NetworkIdentity();
					ImultitoolSlaveable.SetMasterEditor(MasterObject?.GetComponent<IMultitoolMasterable>());
					var MasterObjectName = MasterObject == null ? "null" : MasterObject.OrNull()?.name;

					UIManager.Instance.adminChatWindows.adminLogWindow.ServerAddChatRecord(
						$"{SentByPlayer.Username} Set the master of {ImultitoolSlaveable as MonoBehaviour} at {(ImultitoolSlaveable as MonoBehaviour).transform.position} to {MasterObjectName}", SentByPlayer.UserId);
				}
			}
		}

	}
}