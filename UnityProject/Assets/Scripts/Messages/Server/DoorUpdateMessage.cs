using System.Collections;
using System;
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
			public bool PanelOpen;
			public override string ToString() {
				return $"[DoorUpdateMessage {nameof( Door )}: {Door}, {nameof( Type )}: {Type}]";
			}
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Door);
			if (NetworkObject == null)  return;

			//old doors //needed for fire locks and directional doors
			var doorAnimator = NetworkObject.GetComponent<DoorAnimator>();
			if (doorAnimator != null)
			{
				if (doorAnimator.isActiveAndEnabled == false) return;

				doorAnimator.PlayAnimation(msg.Type, msg.SkipAnimation);
				return;
			}

			//new doors
			var doorMasterController = NetworkObject.GetComponent<DoorMasterController>();
			if (doorMasterController != null)
			{
				var doorAnimatorV2 = doorMasterController.DoorAnimator;
				doorAnimatorV2.PlayAnimation(msg.Type, msg.SkipAnimation, msg.PanelOpen);
				if (msg.Type == DoorUpdateType.Open)
				{
					doorMasterController.BoxCollToggleOff();
				}
				else if (msg.Type == DoorUpdateType.Close)
				{
					doorMasterController.BoxCollToggleOn();
				}
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
		public static NetMessage Send( NetworkConnection recipient, GameObject door, DoorUpdateType type, bool skipAnimation = false, bool PanelOpen = false)
		{
			var msg = new NetMessage
			{
				Door = door.NetId(),
				Type = type,
				SkipAnimation = skipAnimation,
				PanelOpen = PanelOpen
			};

			SendTo(recipient, msg);
			return msg;
		}

		public static NetMessage SendToAll( GameObject door, DoorUpdateType type , bool PanelOpen = false)
		{
			var msg = new NetMessage
			{
				Door = door.NetId(),
				Type = type,
				SkipAnimation = false,
				PanelOpen = PanelOpen
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