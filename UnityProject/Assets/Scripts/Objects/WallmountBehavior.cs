using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior common to all wall mounts (such as visibility checking)
/// </summary>
public class WallmountBehavior : MonoBehaviour {

	// Update is called once per frame
	void Update () {
		//don't run check until player is created
		if (PlayerManager.LocalPlayer == null)
		{
			return;
		}
		//TODO: Only update on player move
		//TODO: Only run the check if it's currently on screen, avoid wastefully running this logic every update.

		//decide if we should be visible based on player position
		var spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		//we can ignore this check if we are already visible
		foreach (SpriteRenderer spriteRenderer in spriteRenderers)
		{
			Vector3 headingToPlayer = PlayerManager.LocalPlayer.transform.position - transform.position;
			Vector3 facing = transform.up;
			float difference = Vector3.Angle(facing, headingToPlayer);
				
			spriteRenderer.enabled = difference > 90 || difference < -90;
		}
	}
}
