using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     This Server to Client message is sent when a player is stored inside a closet or crate.
///     It makes sure the relevant ClosetHandler is created on the client to monitor the players actions while the
///     player is being hidden inside
/// </summary>
public class ClosetHandlerMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ClosetHandlerMessage;
	public NetworkInstanceId Closet;

	public override IEnumerator Process()
	{
		if ( Closet == NetworkInstanceId.Invalid )
		{
			yield return null;
		}
		else
		{
			yield return WaitFor(Closet);
		}
		var closetObject = NetworkObject;

		if (!PlayerManager.LocalPlayerScript.IsGhost)
		{
			Transform newTarget = closetObject ? closetObject.transform : PlayerManager.LocalPlayer.transform;
			Camera2DFollow.followControl.target = newTarget;
			//todo: check if it's still required (on live server)
//			Camera2DFollow.followControl.damping = 0.0f;
//			StartCoroutine(WaitForCameraToReachCloset());
		}

	}

//	/// <summary>
//	/// Applies the camera dampening when the camera reaches the closet.
//	/// This makes the camera snap the to closet before making the camera "drag" as the closet moves.
//	/// Snapping the camera to the closet is needed for when a player inside the closet rejoins the game, otherwise the
//	/// camera will move/"drag" from coordinate 0,0 across the station to the closet's position.
//	/// </summary>
//	IEnumerator WaitForCameraToReachCloset()
//	{
//		yield return new WaitUntil(() =>
//			Camera2DFollow.followControl.transform == Camera2DFollow.followControl.target);
//		Camera2DFollow.followControl.damping = 0.2f;
//	}

	public static ClosetHandlerMessage Send(GameObject recipient, GameObject closet)
	{
		ClosetHandlerMessage msg = new ClosetHandlerMessage
		{
			Closet = closet.NetId()
		};
		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[ClosetHandlerMessage Closet={0}]", Closet);
	}
}