using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

/// <summary>
/// Behavior common to all wall mounts. Note that a wallmount's facing is determined by the Directional.InitialDirection,
/// but the wallmount sprites are always re-oriented to be upright when in game.
///
/// Adds a WallmountSpriteBehavior to all child objects that have SpriteRenderers. Facing / visibility checking is handled in
/// there. See <see cref="WallmountSpriteBehavior"/>
/// </summary>
[RequireComponent(typeof(Directional))]
public class WallmountBehavior : MonoBehaviour, IMatrixRotation
{
	//cached spriteRenderers of this gameobject
	private SpriteRenderer[] spriteRenderers;
	public Directional directional;
	private Transform child;
	private Vector3 upVectorForRotation;

	private void Start()
	{
		upVectorForRotation = transform.up;
		child = transform.GetChild(0);
		directional = GetComponent<Directional>();
		spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
		foreach (SpriteRenderer renderer in spriteRenderers)
		{
			//don't add it if it already exists
			var existingRenderer = renderer.GetComponent<WallmountSpriteBehavior>();
			if (existingRenderer == null)
			{
				renderer.gameObject.AddComponent<WallmountSpriteBehavior>();
			}
		}
	}

	void OnDrawGizmos ()
	{
		if (Application.isEditor && Application.isPlaying)
		{
			//shows calcualted facing, even during matrix rotations
			Gizmos.color = Color.red;
			DebugGizmoUtils.DrawArrow(transform.position, CalculateFacing());
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


		Vector3 facing = -CalculateFacing();
		float difference = Vector3.Angle(facing, headingToPosition);
		//91 rather than 90 helps prevent flickering due to rounding
		return difference >= 91 || difference <= -91;
	}

	public Vector3 CalculateFacing()
	{
		//when a matrix is static, Directional can be used to determine facing, but when it is rotating,
		//directional always points in a cardinal direction which doesn't match the actual facing.
		//so we use the offset from its up direction prior to rotation to determine the offset
		return Quaternion.Euler(0, 0,  Vector2.SignedAngle(upVectorForRotation, transform.up)) * directional.CurrentDirection.Vector;
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

	public void OnMatrixRotate(MatrixRotationInfo rotationInfo)
	{
		if (rotationInfo.IsClientside)
		{
			//cache the upwards direction so we can use it to determine angle offset during a matrix rotation
			upVectorForRotation = transform.up;
		}
	}
}
