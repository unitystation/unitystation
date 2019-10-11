using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This handles the cone of sight
/// raycasting for NPC Mobs
/// </summary>
public class ConeOfSight : MonoBehaviour
{
	[Range(0, 360)] [Header("Field of View in Degrees")]
	public float fieldOfView = 90f;

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
			var offset = Mathf.Lerp(-offsetDegrees, offsetDegrees, (float)i / (float)rayCount - 1);
			var castDir = (Quaternion.AngleAxis(-angleOfDir, Vector3.forward) * Quaternion.Euler(0,0, -offset)) * Vector3.up;

			RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir, lengthOfSight, hitMask);

			if (hit.collider != null)
			{
				hitColls.Add(hit.collider);
			}
		}

		return hitColls;
	}
}