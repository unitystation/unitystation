using System.Collections;
using Doors;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	public class DoorUpdateMessage : ServerMessage<DoorUpdateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public DoorUpdateType Type;
			public uint Door;
			// whether the update should occur instantaneously
			public bool SkipAnimation;

			public override string ToString() {
				return $"[DoorUpdateMessage {nameof( Door )}: {Door}, {nameof( Type )}: {Type}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Door);

			if ( NetworkObject != null ) {
				NetworkObject.GetComponent<DoorAnimator>()?.PlayAnimation( msg.Type, msg.SkipAnimation );
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
		public static NetMessage Send( NetworkConnection recipient, GameObject door, DoorUpdateType type, bool skipAnimation = false )
		{
			var msg = new NetMessage
			{
				Door = door.NetId(),
				Type = type,
				SkipAnimation = skipAnimation
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll( GameObject door, DoorUpdateType type )
		{
			var msg = new NetMessage
			{
				Door = door.NetId(),
				Type = type,
				SkipAnimation = false
			};

			SendToAll(msg);
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
}