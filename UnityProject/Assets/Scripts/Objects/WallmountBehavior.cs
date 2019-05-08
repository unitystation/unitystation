﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Behavior common to all wall mounts.
/// Behavior common to all wall mounts. Note that a wallmount's facing is determined by the gameobject's rotation,
/// but the wallmount sprites are always re-oriented to be upright when in game.
///
/// Adds a WallmountSpriteBehavior to all child objects that have SpriteRenderers. Facing / visibility checking is handled in
/// there. See <see cref="WallmountSpriteBehavior"/>
/// </summary>
public class WallmountBehavior : MonoBehaviour
{
	//cached spriteRenderers of this gameobject
	private SpriteRenderer[] spriteRenderers;

	private void Start()
	{
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer renderer in spriteRenderers)
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

	void OnDrawGizmos ()
	{
		Gizmos.color = Color.green;
		DebugGizmoUtils.DrawArrow(transform.position, - transform.up);
	}

	/// <summary>
	/// Checks if the wallmount has been hidden based on facing calculation already performed. Use this
	/// to avoid having to re-calculate facing.
	/// </summary>
	/// <returns>true iff this wallmount has been already hidden due to not facing the local player</returns>
	public bool IsHiddenFromLocalPlayer()
	{
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			if (renderer.color.a > 0)
			{
				//there's at least one non-transparent renderer, so it's not hidden
				return false;
			}
		}

		//there were no renderers or all of them were transparent, it's hidden
		return true;
	}
}
