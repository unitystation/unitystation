using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Util;

public class DoorUpdateMessage : ServerMessage {
	public static short MessageType = (short) MessageTypes.DoorUpdateMessage;
 
	public NetworkInstanceId Door;

	public override IEnumerator Process() {
//		Debug.Log("Processed " + ToString());
		yield return WaitFor( Door );
	}

	public override string ToString() {
		return $"[DoorUpdateMessage {nameof( Door )}: {Door}]";
	}


	public static DoorUpdateMessage Send( GameObject recipient, GameObject door/*, Event ??? type*/) {

		var msg = new DoorUpdateMessage {
			Door = door.NetId(),
//			Type = type,
		};
		msg.SendTo( recipient );
		return msg;
	}
}