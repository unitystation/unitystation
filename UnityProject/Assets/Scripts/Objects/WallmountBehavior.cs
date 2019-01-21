using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Behavior common to all wall mounts.
///
/// Adds a WallmountSpriteBehavior to all child objects that have SpriteRenderers. Facing / visibility checking is handled in
/// there. See <see cref="WallmountSpriteBehavior"/>
/// </summary>
public class WallmountBehavior : MonoBehaviour {
	private void Start()
	{
		//add the behavior to all child spriterenderers
		foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>())
		{
			renderer.gameObject.AddComponent<WallmountSpriteBehavior>();
		}
	}

	/// <summary>
	/// Checks if the wallmount is facing the specified position
	/// </summary>
	/// <param name="worldPosition">position to check</param>
	/// <returns>true iff it is facing the position</returns>
	public bool IsFacingPosition(Vector3 worldPosition)
	{
		Vector3 headingToPosition = worldPosition - transform.position;
		Vector3 facing = transform.up;
		float difference = Vector3.Angle(facing, headingToPosition);
		//91 rather than 90 helps prevent flickering due to rounding
		return difference >= 91 || difference <= -91;
	}
}
