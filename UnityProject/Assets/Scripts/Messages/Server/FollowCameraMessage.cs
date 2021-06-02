using Systems.Ai;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	///     This Server to Client message is sent when a player is stored inside a closet or crate, or needs to follow some other object.
	/// </summary>
	public class FollowCameraMessage : ServerMessage<FollowCameraMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ObjectToFollow;

			public override string ToString()
			{
				return string.Format("[FollowCameraMessage ObjectToFollow={0}]", ObjectToFollow);
			}
		}

		public override void Process(NetMessage msg)
		{
			if ( msg.ObjectToFollow == NetId.Invalid )
			{
				return;
			}
			else
			{
				LoadNetworkObject(msg.ObjectToFollow);
			}
			var objectToFollow = NetworkObject;

			if (!PlayerManager.LocalPlayerScript.IsGhost)
			{
				Transform newTarget = objectToFollow ? objectToFollow.transform : PlayerManager.LocalPlayer.transform;
				Camera2DFollow.followControl.target = newTarget;
			}
		}

		public static NetMessage Send(GameObject recipient, GameObject objectToFollow)
		{
			NetMessage msg = new NetMessage
			{
				ObjectToFollow = objectToFollow.NetId()
			};

			SendTo(recipient, msg);
			return msg;
		}
	}

	/// <summary>
	/// This Server to Client message is sent when an Ai player needs their camera moving.
	/// </summary>
	public class FollowCameraAiMessage : ServerMessage<FollowCameraAiMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint ObjectToFollow;

			public override string ToString()
			{
				return string.Format("[FollowCameraMessage ObjectToFollow={0}]", ObjectToFollow);
			}
		}

		public override void Process(NetMessage msg)
		{
			if ( msg.ObjectToFollow == NetId.Invalid )
			{
				return;
			}

			LoadNetworkObject(msg.ObjectToFollow);
			var objectToFollow = NetworkObject;

			//Only follow stuff if we are Ai object
			if(PlayerManager.LocalPlayer.TryGetComponent<AiPlayer>(out var aiPlayer) == false) return;

			//Follow new object if its not null
			if (objectToFollow != null)
			{
				aiPlayer.ClientSetCameraLocation(objectToFollow.transform);
				return;
			}

			//Otherwise try to follow core
			if (aiPlayer.VesselObject != null)
			{
				aiPlayer.ClientSetCameraLocation(aiPlayer.VesselObject.transform);
			}

			//Otherwise we must be dead so do nothing
		}

		public static NetMessage Send(GameObject recipient, GameObject objectToFollow)
		{
			NetMessage msg = new NetMessage
			{
				ObjectToFollow = objectToFollow.NetId()
			};

			SendTo(recipient, msg);
			return msg;
		}
	}
}