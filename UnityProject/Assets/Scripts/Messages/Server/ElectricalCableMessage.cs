using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
/// <summary>
/// Used to update the ends, and colour of  A cable
/// </summary>
public class ElectricalCableMessage : ServerMessage
{
	public class ElectricalCableMessageNetMessage : NetworkMessage
	{
		public Connection REWireEndA;
		public Connection REWireEndB;
		public WiringColor RECableType;
		public uint Cable;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as ElectricalCableMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.Cable);

		if ( NetworkObject != null)
		{
			NetworkObject.GetComponent<CableInheritance>()?.SetDirection(newMsg.REWireEndA, newMsg.REWireEndB, newMsg.RECableType);
		}
	}

	public static ElectricalCableMessageNetMessage  Send(GameObject cable, Connection WireEndA, Connection WireEndB, WiringColor CableType = WiringColor.unknown)
	{
		ElectricalCableMessageNetMessage msg = new ElectricalCableMessageNetMessage
		{
			REWireEndA = WireEndA,
			REWireEndB = WireEndB,
			RECableType = CableType,
			Cable = cable.NetId()
		};
		new ElectricalCableMessage().SendToAll(msg);
		return msg;
	}
}
