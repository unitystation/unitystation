using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior common to all wall mounts.
///
/// Looks at the way a wallmount is facing by using the gameobject's transform.up (NOT the sprite's transform). Make sure the gameobject is set up so
/// the transform.up properly points in the direction the object is facing (modify the z coordinate of the rotation to adjust facing).
/// You may need to adjust the sprite rotation as well as the parent game object rotation if the object has unusual facing behavior such as
/// the Request Console.
///
/// Wallmount sprites are occluded like all the other objects, however they should only be visible on the
/// side of the wall that they are on. The occlusion logic keeps the nearest wall visible so we must
/// check which side of the wall the mount is on so we can hide it if it's on the wrong side.
/// This behavior makes the wallmount invisible if it is not facing towards the player.
/// Facing is calculated based
/// </summary>
public class WallmountBehavior : MonoBehaviour {
	private bool mHiddenByFacing = false;

	// Update is called once per frame
	void Update () {
		//don't run check until player is created
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}

		//check if we even need to update the visibility
		//are any sprite renderers visible?
		bool renderersVisible = false;
		foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
		{
			if (spriteRenderer.enabled)
			{
				renderersVisible = true;
				break;
			}
		}

		if (renderersVisible || !renderersVisible && mHiddenByFacing)
		{
			//sprites are visible currently or are invisible but only due to this component,
			//re-calculate if the sprite should be hidden due to facing
			Vector3 headingToPlayer = PlayerManager.LocalPlayer.transform.position - transform.position;
			Vector3 facing = transform.up;
			float difference = Vector3.Angle(facing, headingToPlayer);
			mHiddenByFacing = !(difference > 90 || difference < -90);

			foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>())
			{
				spriteRenderer.enabled = !mHiddenByFacing;
			}
		}
	}
}
