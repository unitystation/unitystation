using Mirror;
using UnityEngine;
using Objects.Electrical;

namespace Messages.Server
{
	/// <summary>
	/// Used to update the ends, and colour of  A cable
	/// </summary>
	public class ElectricalCableMessage : ServerMessage<ElectricalCableMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public Connection REWireEndA;
			public Connection REWireEndB;
			public WiringColor RECableType;
			public uint Cable;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Cable);

			if ( NetworkObject != null)
			{
				NetworkObject.GetComponent<CableInheritance>()?.SetDirection(msg.REWireEndA, msg.REWireEndB, msg.RECableType);
			}
		}

		public static NetMessage  Send(GameObject cable, Connection WireEndA, Connection WireEndB, WiringColor CableType = WiringColor.unknown)
		{
			NetMessage msg = new NetMessage
			{
				REWireEndA = WireEndA,
				REWireEndB = WireEndB,
				RECableType = CableType,
				Cable = cable.NetId()
			};

			SendToAll(msg);
			return msg;
		}
	}
}
