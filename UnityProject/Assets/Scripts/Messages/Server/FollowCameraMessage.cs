using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     This Server to Client message is sent when a player is stored inside a closet or crate, or needs to follow some other object.
/// </summary>
public class FollowCameraMessage : ServerMessage
{
	public class FollowCameraMessageNetMessage : NetworkMessage
	{
		public uint ObjectToFollow;

		public override string ToString()
		{
			return string.Format("[FollowCameraMessage ObjectToFollow={0}]", ObjectToFollow);
		}
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as FollowCameraMessageNetMessage;
		if(newMsg == null) return;

		if ( newMsg.ObjectToFollow == NetId.Invalid )
		{
			return;
		}
		else
		{
			LoadNetworkObject(newMsg.ObjectToFollow);
		}
		var objectToFollow = NetworkObject;

		if (!PlayerManager.LocalPlayerScript.IsGhost)
		{
			Transform newTarget = objectToFollow ? objectToFollow.transform : PlayerManager.LocalPlayer.transform;
			Camera2DFollow.followControl.target = newTarget;
		}
	}

	public static FollowCameraMessageNetMessage Send(GameObject recipient, GameObject objectToFollow)
	{
		FollowCameraMessageNetMessage msg = new FollowCameraMessageNetMessage
		{
			ObjectToFollow = objectToFollow.NetId()
		};

		new FollowCameraMessage().SendTo(recipient, msg);
		return msg;
	}
}