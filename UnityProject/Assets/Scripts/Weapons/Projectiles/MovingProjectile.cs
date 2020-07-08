using Container.Gun;
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

		private Vector3 mPrevPos;

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

			var startPosition =  Vector3.zero;
			var startRotation = Quaternion.AngleAxis(-Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg, Vector3.forward);

			thisTransform.localPosition = startPosition;
			thisTransform.rotation = startRotation;
		}

		private void Update()
		{
			CachePreviousPosition();

			MoveBullet();

			var hit = Raycast();

			projectile.ProcessRaycastHit(hit);
		}

		private void CachePreviousPosition()
		{
			mPrevPos = thisTransform.position;
		}

		/// <summary>
		/// Moves child game object in local space of the bullet
		/// </summary>
		private void MoveBullet()
		{
			var distanceToTravel = Vector2.up * (velocity * Time.deltaTime);
			thisTransform.Translate(distanceToTravel, Space.Self);
		}

		/// <summary>
		/// Raycasts line from previous bullet position in direction of the bullet
		/// </summary>
		/// <returns></returns>
		private RaycastHit2D Raycast()
		{
			var distanceDelta = thisTransform.position - mPrevPos;
			var hit = Physics2D.Raycast(mPrevPos, distanceDelta.normalized, distanceDelta.magnitude, maskData.Layers);
			return hit;
		}
	}
}