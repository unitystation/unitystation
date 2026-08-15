using UnityEngine;
using US13.Managers.NetworkManagement;
using Util;

namespace US13.Projectiles
{
	public class TrackingMovingProjectile : MovingProjectile
	{
		public GameObject Target;

		protected override void UpdateMe()
		{
			if (CustomNetworkManager.IsServer == false) return;
			if (projectile.Destroyed) return;

			CachePreviousPosition();

			if (ProcessMovement(MoveProjectileToTarget()))
			{
				SimulateCollision();
			}
		}

		protected Vector2 MoveProjectileToTarget()
		{
			if (Target != null)
			{
				RotateTowardsTarget();
			}

			var distanceToTravel = Vector2.up * (velocity * Time.deltaTime);
			ProjectileTransform.Translate(distanceToTravel, Space.Self);
			var worldPos = ProjectileTransform.position;
			//NOTE Needs to be world since the client doesn't have the Prefab parented to anything
			SyncPosition(currentLocalPosition, worldPos);
			return distanceToTravel;
		}

		private void RotateTowardsTarget()
		{
			Vector2 directionToTarget = (Vector2) Target.gameObject.AssumedWorldPosServer() -
			                            (Vector2) ProjectileTransform.position;
			if (directionToTarget.sqrMagnitude < 0.0001f) return; // avoid NaN from atan2 on a zero vector

			float targetAngle =
				Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg -
				90f; // -90 since Vector2.up is treated as forward
			ProjectileTransform.rotation = Quaternion.Euler(0f, 0f, targetAngle);
		}
	}
}