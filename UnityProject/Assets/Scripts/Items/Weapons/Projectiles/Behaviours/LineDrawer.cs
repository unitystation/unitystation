using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Draws a line from shooter to hit position or end
	/// </summary>
	[RequireComponent(typeof(LineRenderer))]
	public class LineDrawer : MonoBehaviour, IOnShoot, IOnDespawn
	{
		private Transform thisTransform;
		private LineRenderer lineRenderer;

		private Vector3 direction;

		private void Awake()
		{
			thisTransform = transform;
			lineRenderer = GetComponent<LineRenderer>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
		}

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			var pos = thisTransform.position;
			Vector3 startPos = new Vector3(direction.x, direction.y, pos.z) * 0.7f;

			if (hit.CollisionHit.GameObject == null)
			{
				lineRenderer.SetPosition(0, pos + startPos);
				lineRenderer.SetPosition(1, point);
				return;
			}

			var endPosition = hit.HitWorld;
			lineRenderer.SetPosition(0, pos + startPos);
			lineRenderer.SetPosition(1, endPosition);
		}

		private void OnDisable()
		{
			direction = Vector3.zero;
		}
	}
}