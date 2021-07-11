using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles
{
	/// <summary>
	/// This script is used for actually moving projectiles sprite and raycasting
	/// Reason why it was done in local space and on a separate game object child it this
	/// LocalTrailRanderer cannot properly draw line in World space if it's done on a moving matrix
	/// </summary>
	public class MovingProjectile : MonoBehaviour
	{
		private Bullet projectile;
		private LayerMaskData maskData;
		private Transform thisTransform;

		private Vector3 previousPosition;

		private float velocity;

		private void Awake()
		{

			projectile = GetComponentInParent<Bullet>();
			maskData = projectile.MaskData;
			thisTransform = transform;
		}

		/// <summary>
		/// Method to rotate and reset projectile position in local space
		/// </summary>
		/// <param name="direction"> Direction to travel </param>
		/// <param name="velocity"> Projectile speed </param>
		public void SetUpBulletTransform(Vector2 direction, float velocity)
		{
			this.velocity = velocity;

			thisTransform.rotation =
				Quaternion.AngleAxis(
					-Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg,
					Vector3.forward);
		}

		private void Update()
		{
			if(projectile.Destroyed) return;

			CachePreviousPosition();

			if (ProcessMovement( MoveProjectile()))
			{
				SimulateCollision();
			}
		}

		private void CachePreviousPosition()
		{
			previousPosition =  thisTransform.position;
		}

		private Vector2 MoveProjectile()
		{
			var distanceToTravel = Vector2.up * (velocity * Time.deltaTime);
			thisTransform.Translate(distanceToTravel, Space.Self);
			return distanceToTravel;
		}

		private bool ProcessMovement(Vector2 distanceToTravel)
		{
			return projectile.ProcessMove(distanceToTravel, thisTransform.position, previousPosition);
		}

		private void SimulateCollision()
		{
			var distanceDelta = thisTransform.position - previousPosition;
			var hit = MatrixManager.RayCast(previousPosition, distanceDelta.normalized, distanceDelta.magnitude,maskData.TileMapLayers ,maskData.Layers);

			projectile.ProcessRaycastHit(hit);
		}

		private void OnDisable()
		{
			thisTransform.localPosition = Vector3.zero;
			previousPosition = Vector3.zero;
			velocity = 0;
		}
	}
}