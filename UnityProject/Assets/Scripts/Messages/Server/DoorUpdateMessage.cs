using System.Collections;
using UnityEngine;
using Mirror;
using Doors;

public class DoorUpdateMessage : ServerMessage
{
	public class DoorUpdateMessageNetMessage : ActualMessage
	{
		public DoorUpdateType Type;
		public uint Door;
		// whether the update should occur instantaneously
		public bool SkipAnimation;

		public override string ToString() {
			return $"[DoorUpdateMessage {nameof( Door )}: {Door}, {nameof( Type )}: {Type}]";
		}
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as DoorUpdateMessageNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.Door);

		if ( NetworkObject != null ) {
			NetworkObject.GetComponent<DoorAnimator>()?.PlayAnimation( newMsg.Type, newMsg.SkipAnimation );
		}
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
	public static DoorUpdateMessageNetMessage Send( NetworkConnection recipient, GameObject door, DoorUpdateType type, bool skipAnimation = false )
	{
		var msg = new DoorUpdateMessageNetMessage
		{
			Door = door.NetId(),
			Type = type,
			SkipAnimation = skipAnimation
		};

		new DoorUpdateMessage().SendTo(recipient, msg);
		return msg;
	}

	public static DoorUpdateMessageNetMessage SendToAll( GameObject door, DoorUpdateType type )
	{
		var msg = new DoorUpdateMessageNetMessage
		{
			Door = door.NetId(),
			Type = type,
			SkipAnimation = false
		};

		new DoorUpdateMessage().SendToAll(msg);
		return msg;
	}
}

public enum DoorUpdateType
{
	Open = 0,
	Close = 1,
	AccessDenied = 2,
	PressureWarn = 3
}