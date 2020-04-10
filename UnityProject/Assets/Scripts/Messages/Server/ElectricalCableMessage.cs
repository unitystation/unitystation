using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
/// <summary>
/// Used to update the ends, and colour of  A cable
/// </summary>
public class ElectricalCableMessage : ServerMessage
{
	public Connection REWireEndA;
	public Connection REWireEndB;
	public WiringColor RECableType;
	public uint Cable;

	public override void Process()
	{
		LoadNetworkObject(Cable);

		if ( NetworkObject != null)
		{
			NetworkObject.GetComponent<CableInheritance>()?.SetDirection(REWireEndA,REWireEndB,RECableType);
		}
	}

	public static ElectricalCableMessage  Send(GameObject cable, Connection WireEndA, Connection WireEndB, WiringColor CableType = WiringColor.unknown)
	{
		ElectricalCableMessage msg = new ElectricalCableMessage
		{
			REWireEndA = WireEndA,
			REWireEndB = WireEndB,
			RECableType = CableType,
			Cable = cable.NetId()
		};
		msg.SendToAll();
		return msg;
	}
}
