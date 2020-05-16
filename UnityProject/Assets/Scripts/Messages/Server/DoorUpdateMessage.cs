using System.Collections;
using UnityEngine;
using Mirror;

public class DoorUpdateMessage : ServerMessage {

	public DoorUpdateType Type;
	public uint Door;
	// whether the update should occur instantaneously
	public bool SkipAnimation;

	public override void Process() {
//		Logger.Log("Processed " + ToString());
		LoadNetworkObject(Door);

		if ( NetworkObject != null ) {
			NetworkObject.GetComponent<DoorAnimator>()?.PlayAnimation( Type, SkipAnimation );
		}
	}

	public override string ToString() {
		return $"[DoorUpdateMessage {nameof( Door )}: {Door}, {nameof( Type )}: {Type}]";
	}

	/// <summary>
	/// Send update to one player
	/// </summary>
	/// <param name="recipient">gameobject of the player recieving it</param>
	/// <param name="door">door being updated</param>
	/// <param name="type">animation to play</param>
	/// <param name="skipAnimation">if true, all sound and animations will be skipped, leaving it in its end position.
	/// 	Currently only used for when players are joining and there are open doors.</param>
	/// <returns></returns>
	public static DoorUpdateMessage Send( NetworkConnection recipient, GameObject door, DoorUpdateType type, bool skipAnimation = false ) {
		var msg = new DoorUpdateMessage {
			Door = door.NetId(),
			Type = type,
			SkipAnimation = skipAnimation
		};
		msg.SendTo( recipient );
		return msg;
	}

	public static DoorUpdateMessage SendToAll( GameObject door, DoorUpdateType type ) {
		var msg = new DoorUpdateMessage {
			Door = door.NetId(),
			Type = type,
			SkipAnimation = false
		};
		msg.SendToAll();
		return msg;
	}
}

public enum DoorUpdateType {
	Open = 0,
	Close = 1,
	AccessDenied = 2,
	PressureWarn = 3
}