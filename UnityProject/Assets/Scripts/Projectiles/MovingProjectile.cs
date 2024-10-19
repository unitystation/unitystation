using Mirror;
 using System;
using ScriptableObjects.Gun;
using Tiles;
using UnityEngine;

namespace Weapons.Projectiles
{
	/// <summary>
	/// This script is used for actually moving projectiles sprite and raycasting
	/// Reason why it was done in local space and on a separate game object child it this
	/// LocalTrailRanderer cannot properly draw line in World space if it's done on a moving matrix
	/// </summary>
	public class MovingProjectile : NetworkBehaviour
	{
		private Bullet projectile;
		private LayerMaskData maskData;
		private Transform ProjectileTransform;

		private Vector3 previousPosition;

		[SyncVar(hook = nameof(SyncPosition))]
		private Vector3 currentLocalPosition;

		[SyncVar(hook = nameof(SyncRotation))]
		private Quaternion rotation;

		private float velocity;

		[SerializeField]
		private LayerTile[] tileNamesToIgnore;

		private void Awake()
		{
			projectile = GetComponentInParent<Bullet>();
			maskData = projectile.MaskData;
			ProjectileTransform = this.transform;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		/// <summary>
		/// Method to rotate and reset projectile position in local space
		/// </summary>
		/// <param name="direction"> Direction to travel </param>
		/// <param name="velocity"> Projectile speed </param>
		public void SetUpBulletTransform(Vector2 direction, float velocity)
		{
			this.velocity = velocity;
			SyncRotation(rotation, Quaternion.AngleAxis(
				-Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg,
				Vector3.forward));
		}

		private void UpdateMe()
		{
			if(CustomNetworkManager.IsServer == false) return;
			if(projectile.Destroyed) return;

			CachePreviousPosition();

			if (ProcessMovement( MoveProjectile()))
			{
				SimulateCollision();
			}
		}

		private void CachePreviousPosition()
		{
			previousPosition =  ProjectileTransform.position;
		}

		private Vector2 MoveProjectile()
		{
			var distanceToTravel =  Vector2.up * (velocity * Time.deltaTime);
			ProjectileTransform.Translate(distanceToTravel, Space.Self);
			var Target = ProjectileTransform.position;
			//NOTE Needs to be world since the client doesn't have the Prefab parented to anything
			SyncPosition(currentLocalPosition, Target);
			return distanceToTravel;
		}

		private void SyncRotation(Quaternion InOld, Quaternion InNew)
		{
			rotation = InNew;
			ProjectileTransform.rotation = InNew;
		}


		private void SyncPosition(Vector3 InOld, Vector3 InNew)
		{
			currentLocalPosition = InNew;
			if (isServer) return;
			ProjectileTransform.position = InNew;
		}

		private bool ProcessMovement(Vector2 distanceToTravel)
		{
			return projectile.ProcessMove(distanceToTravel, ProjectileTransform.position, previousPosition);
		}

		private void SimulateCollision()
		{
			var distanceDelta = ProjectileTransform.position - previousPosition;
			var hit = MatrixManager.RayCast(previousPosition, distanceDelta.normalized, distanceDelta.magnitude, maskData.TileMapLayers, maskData.Layers, tileNamesToIgnore: tileNamesToIgnore);

			projectile.ProcessRaycastHit(hit);
		}

		private void OnDisable()
		{
			ProjectileTransform.localPosition = Vector3.zero;
			previousPosition = Vector3.zero;
			velocity = 0;
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}
	}
}