using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     This Server to Client message is sent when a player is stored inside a closet or crate, or needs to follow some other object.
/// </summary>
public class FollowCameraMessage : ServerMessage
{
	public uint ObjectToFollow;

	public override void Process()
	{
		if ( ObjectToFollow == NetId.Invalid )
		{
			return;
		}
		else
		{
			LoadNetworkObject(ObjectToFollow);
		}
		var objectToFollow = NetworkObject;

		if (!PlayerManager.LocalPlayerScript.IsGhost)
		{
			Transform newTarget = objectToFollow ? objectToFollow.transform : PlayerManager.LocalPlayer.transform;
			Camera2DFollow.Instance.target = newTarget;
		}
	}

	public static FollowCameraMessage Send(GameObject recipient, GameObject objectToFollow)
	{
		FollowCameraMessage msg = new FollowCameraMessage
		{
			ObjectToFollow = objectToFollow.NetId()
		};
		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[FollowCameraMessage ObjectToFollow={0}]", ObjectToFollow);
	}
}