﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ElectricalCableMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ElectricalCableMessage;
	public Connection REWireEndA;
	public Connection REWireEndB;
	public WiringColor RECableType;
	public NetworkInstanceId Cable;

	public override IEnumerator Process()
	{
		yield return WaitFor(Cable);

		if ( NetworkObject != null)
		{
			NetworkObject.GetComponent<CableInheritance>()?.SetDirection(REWireEndA,REWireEndB,RECableType);
		}
		yield return null;
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
