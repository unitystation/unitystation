using System.Collections.Generic;
using System.Linq;
using Messages.Server;
using Mirror;
using Newtonsoft.Json;
using Shared.Systems.ObjectConnection;
using UnityEngine;

public class DeviceUpdateLinkerInfoMessage : ServerMessage<DeviceUpdateLinkerInfoMessage.NetMessage>
{
	public struct NetMessage : NetworkMessage
	{
		public string ConnectedDevices;
		public uint MasterID;
	}

	public override void Process(NetMessage msg)
	{
		var Slaves = JsonConvert.DeserializeObject<List<uint>>(msg.ConnectedDevices);
		InGameDeviceLinker.Instance.MastersData.Clear();
		var Master = msg.MasterID.NetworkIdentity().GetComponent<IMultitoolMasterable>();

		InGameDeviceLinker.Instance.MastersData[Master.ConType] = new Dictionary<IMultitoolMasterable, List<IMultitoolSlaveable>>();
		InGameDeviceLinker.Instance.MastersData[Master.ConType][Master] = new List<IMultitoolSlaveable>();


		foreach (var Slave in Slaves)
		{
			foreach (var iMultitoolSlaveable in Slave.NetworkIdentity().GetComponents<IMultitoolSlaveable>())
			{
				if (iMultitoolSlaveable.ConType == Master.ConType)
				{
					InGameDeviceLinker.Instance.MastersData[Master.ConType][Master].Add(iMultitoolSlaveable);
				}
			}

		}

		InGameDeviceLinker.Instance.cursorObject.SelectedMaster = null;
		InGameDeviceLinker.Instance.TrySelect(Master.gameObject, true);
	}

	public static NetMessage SendTo(GameObject recipient, List<IMultitoolSlaveable> Slaves, GameObject Master)
	{
		NetMessage msg = new NetMessage
		{
			ConnectedDevices =  JsonConvert.SerializeObject(Slaves.Select(x => x.gameObject.NetId())),
			MasterID = Master.NetId()
		};

		SendTo(recipient, msg);
		return msg;
	}



}
public static class DeviceLinkerInfoMessageReaderWriters
{
	public static DeviceUpdateLinkerInfoMessage.NetMessage Deserialize(this NetworkReader reader)
	{
		var message = new DeviceUpdateLinkerInfoMessage.NetMessage();
		message.MasterID = reader.ReadUInt();
		message.ConnectedDevices = reader.ReadString();
		return message;
	}

	public static void Serialize(this NetworkWriter writer, DeviceUpdateLinkerInfoMessage.NetMessage message)
	{
		writer.WriteUInt(message.MasterID);
		writer.WriteString(message.ConnectedDevices);
	}
}