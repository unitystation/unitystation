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
	public List<MatrixManager.CollisionHit> GetObjectsInSight(LayerMask hitMask,LayerTypeSelection hitLayers , Vector2 direction, float lengthOfSight,
		int rayCount = 5)
	{
		var angleOfDir = Vector3.Angle(direction, transform.up);
		var cw = Vector3.Cross(transform.up, direction).z < 0f;
		if (!cw)
		{
			angleOfDir = -angleOfDir;
		}

		var offsetDegrees = fieldOfView / 2f;
		List<MatrixManager.CollisionHit> hitColls = new List<MatrixManager.CollisionHit>();

		for (int i = 0; i < rayCount; i++)
		{
			var step = (float) i / ((float) rayCount - 1);
			var offset = Mathf.Lerp(-offsetDegrees, offsetDegrees, step);
			var castDir = (Quaternion.AngleAxis(-angleOfDir, Vector3.forward) * Quaternion.Euler(0,0, -offset)) * Vector3.up;

			var hit = MatrixManager.RayCast(transform.position + castDir, castDir, lengthOfSight, hitLayers, hitMask);
			// Debug.DrawRay(transform.position, castDir, Color.blue, 10f);
			if (hit.ItHit)
			{
				hitColls.Add(hit.CollisionHit);
			}
		}

		return hitColls;
	}

	/// <summary>
	/// Returns the furthest unhindered position possible in the cone of sight.
	/// A world position is returned. It is advisable to use this
	/// with matrix manager so all matricies are considered
	/// </summary>
	public Vector2 GetFurthestPositionInSight(Vector2 originWorldPos,LayerTypeSelection layerMask, LayerMask hitMask, Vector2 direction, float lengthOfSight,
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
		var  dirHit = MatrixManager.RayCast(originWorldPos, direction, lengthOfSight, layerMask , hitMask);
	//	Debug.DrawRay(originWorldPos, direction, Color.red, 10f);
		if (dirHit.ItHit == false)
		{
			furthestDist = lengthOfSight;
			furthestPoint = originWorldPos + ((direction * lengthOfSight) - direction);
		}
		else
		{
			furthestDist = dirHit.Distance;
			furthestPoint = originWorldPos + ((direction * dirHit.Distance) - direction);
		}

		//now test all rays in the cone of sight:
		for (int i = 0; i < rayCount; i++)
		{
			var step = (float) i / ((float) rayCount - 1);
			var offset = Mathf.Lerp(-offsetDegrees, offsetDegrees, step);

			var castDir = (Quaternion.AngleAxis(-angleOfDir, Vector3.forward) * Quaternion.Euler(0,0, -offset)) * Vector3.up;

			var hit = MatrixManager.RayCast(originWorldPos, castDir, lengthOfSight, layerMask, hitMask);
		//	Debug.DrawRay(originWorldPos, castDir, Color.blue, 10f);
			if (hit.ItHit)
			{
				if (hit.Distance > furthestDist)
				{
					//this ray is longer, calculate the general position:
					furthestDist = hit.Distance;
					furthestPoint = originWorldPos + (Vector2)((castDir * hit.Distance) - (castDir * 1.5f));
				}
			}
		}

		return furthestPoint;
	}
}
