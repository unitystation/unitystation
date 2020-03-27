using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This handles the cone of sight
/// raycasting for NPC Mobs
/// </summary>
public class ConeOfSight : MonoBehaviour
{
	[Range(0, 360)] [Header("Field of View in Degrees")]
	public float fieldOfView = 180f;

	/// <summary>
	/// Returns all colliders found in the cone of sight
	/// Provide the direction to look and
	/// include the layers you want to test for in the hitMask
	/// </summary>
	public List<Collider2D> GetObjectsInSight(LayerMask hitMask, Vector2 direction, float lengthOfSight,
		int rayCount = 5)
	{
		var angleOfDir = Vector3.Angle(direction, transform.up);
		var cw = Vector3.Cross(transform.up, direction).z < 0f;
		if (!cw)
		{
			angleOfDir = -angleOfDir;
		}

		var offsetDegrees = fieldOfView / 2f;
		List<Collider2D> hitColls = new List<Collider2D>();

		for (int i = 0; i < rayCount; i++)
		{
			var step = (float) i / ((float) rayCount - 1);
			var offset = Mathf.Lerp(-offsetDegrees, offsetDegrees, step);
			var castDir = (Quaternion.AngleAxis(-angleOfDir, Vector3.forward) * Quaternion.Euler(0,0, -offset)) * Vector3.up;

			RaycastHit2D hit = Physics2D.Raycast(transform.position + castDir, castDir, lengthOfSight, hitMask);
			// Debug.DrawRay(transform.position, castDir, Color.blue, 10f);
			if (hit.collider != null)
			{
				hitColls.Add(hit.collider);
			}
		}

		return hitColls;
	}

	/// <summary>
	/// Returns the furthest unhindered position possible in the cone of sight.
	/// A world position is returned. It is advisable to use this
	/// with matrix manager so all matricies are considered
	/// </summary>
	public Vector2 GetFurthestPositionInSight(Vector2 originWorldPos, LayerMask hitMask, Vector2 direction, float lengthOfSight,
		int rayCount = 5)
	{
		var angleOfDir = Vector3.Angle(direction, transform.up);
		var cw = Vector3.Cross(transform.up, direction).z < 0f;
		if (!cw)
		{
			angleOfDir = -angleOfDir;
		}

		var offsetDegrees = fieldOfView / 2f;
		var furthestDist = 0f;
		var furthestPoint = Vector2.zero;

		//First see how far the initial direction is
		RaycastHit2D dirHit = Physics2D.Raycast(originWorldPos, direction, lengthOfSight, hitMask);
	//	Debug.DrawRay(originWorldPos, direction, Color.red, 10f);
		if (dirHit.collider == null)
		{
			furthestDist = lengthOfSight;
			furthestPoint = originWorldPos + ((direction * lengthOfSight) - direction);
		}
		else
		{
			furthestDist = dirHit.distance;
			furthestPoint = originWorldPos + ((direction * dirHit.distance) - direction);
		}

		//now test all rays in the cone of sight:
		for (int i = 0; i < rayCount; i++)
		{
			var step = (float) i / ((float) rayCount - 1);
			var offset = Mathf.Lerp(-offsetDegrees, offsetDegrees, step);

			var castDir = (Quaternion.AngleAxis(-angleOfDir, Vector3.forward) * Quaternion.Euler(0,0, -offset)) * Vector3.up;

			RaycastHit2D hit = Physics2D.Raycast(originWorldPos, castDir, lengthOfSight, hitMask);
		//	Debug.DrawRay(originWorldPos, castDir, Color.blue, 10f);
			if (hit.collider != null)
			{
				if (hit.distance > furthestDist)
				{
					//this ray is longer, calculate the general position:
					furthestDist = hit.distance;
					furthestPoint = originWorldPos + (Vector2)((castDir * hit.distance) - (castDir * 1.5f));
				}
			}
		}

		return furthestPoint;
	}
}