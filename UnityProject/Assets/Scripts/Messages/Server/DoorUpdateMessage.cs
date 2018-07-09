using System.Collections;
using System.Collections.Generic;
using Doors;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using Util;

public class DoorUpdateMessage : ServerMessage {
	public static short MessageType = (short) MessageTypes.DoorUpdateMessage;

	public DoorUpdateType Type;
	public NetworkInstanceId Door;

	public override IEnumerator Process() {
//		Debug.Log("Processed " + ToString());
		yield return WaitFor( Door );
		
		if ( NetworkObject != null ) {
			NetworkObject.GetComponent<DoorAnimator>()?.PlayAnimation( Type );
		}
	}

	public override string ToString() {
		return $"[DoorUpdateMessage {nameof( Door )}: {Door}, {nameof( Type )}: {Type}]";
	}

	public static DoorUpdateMessage Send( GameObject recipient, GameObject door, DoorUpdateType type ) {
		var msg = new DoorUpdateMessage {
			Door = door.NetId(),
			Type = type,
		};
		msg.SendTo( recipient );
		return msg;
	}

	public static DoorUpdateMessage SendToAll( GameObject door, DoorUpdateType type ) {
		var msg = new DoorUpdateMessage {
			Door = door.NetId(),
			Type = type,
		};
		msg.SendToAll();
		return msg;
	}
}

public enum DoorUpdateType {
	Open = 0,
	Close = 1,
	AccessDenied = 2
}