using Logs;
using Mirror;
using UnityEngine;
using US13.Core.Admin.Logs;
using US13.Core.ObjectConnection;
using US13.Messages.Client.Admin;
using US13.Messages.Server;
using US13.UI.Systems.AdminTools.DevTools.InGameDeviceLinker;
using Util;

namespace US13.Messages.Client.DeviceLinker
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
			if (HasPermission(TAG.MAP_LINK) == false) return;

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
					var masterObject = msg.Master.NetworkIdentity();
					ImultitoolSlaveable.SetMasterEditor(masterObject?.GetComponent<IMultitoolMasterable>());
					var masterObjectName = masterObject == null ? "null" : masterObject.OrNull()?.name;
					if (masterObjectName == null)
					{
						Loggy.Error("[DeviceLinKMessage] Master object is null");
						return;
					}
					AdminLogsManager.AddNewLog(SentByPlayer.GameObject, $"{SentByPlayer.Username} Set the master of {ImultitoolSlaveable as MonoBehaviour} at {((ImultitoolSlaveable as MonoBehaviour)!).transform.position} to {masterObjectName}", LogCategory.Admin);
				}
			}
		}

	}
}