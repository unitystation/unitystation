using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Draws a line from shooter to hit position or end
	/// </summary>
	[RequireComponent(typeof(LineRenderer))]
	public class LineDrawer : MonoBehaviour, IOnShoot, IOnDespawn
	{
		private Vector3 direction;
		private LineRenderer lineRenderer;

		private void Awake()
		{
			lineRenderer = GetComponent<LineRenderer>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
		}

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			var pos = transform.position;
			Vector3 startPos = new Vector3(direction.x, direction.y, pos.z) * 0.7f;

			if (hit.collider == null)
			{
				lineRenderer.SetPosition(0, pos + startPos);
				lineRenderer.SetPosition(1, point);
				return;
			}

			var endPosition = hit.point;
			lineRenderer.SetPosition(0, pos + startPos);
			lineRenderer.SetPosition(1, endPosition);
		}
	}
}