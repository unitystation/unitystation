using Container.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;

namespace Weapons.Projectiles
{
	public class Bullet : Projectile
	{
		private IOnShoot[] behavioursOnShoot;
		private IOnMove[] behavioursOnMove;
		private IOnHit[] behavioursOnBulletHit;
		private IOnDespawn[] behavioursOnBulletDespawn;

		private MovingProjectile movingProjectile;

		private Transform thisTransform;
		private GameObject shooter;
		private bool isSuicide;

		[SerializeField] private HitProcessor hitProcessor = null;
		[SerializeField] private LayerMaskData maskData = null;

		public LayerMaskData MaskData => maskData;

		private void Awake()
		{
			behavioursOnShoot = GetComponents<IOnShoot>();
			behavioursOnMove = GetComponentsInParent<IOnMove>();
			behavioursOnBulletHit = GetComponents<IOnHit>();
			behavioursOnBulletDespawn = GetComponents<IOnDespawn>();

			movingProjectile = GetComponentInChildren<MovingProjectile>();

			thisTransform = transform;
		}

		public override void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			isSuicide = true;
			StartShoot(Vector2.zero, controlledByPlayer, fromWeapon, targetZone);
		}

		public override void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			isSuicide = false;
			StartShoot(direction, controlledByPlayer, fromWeapon, targetZone);
		}

		private void StartShoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone)
		{
			shooter = controlledByPlayer;

			thisTransform.parent = controlledByPlayer.transform.parent;

			var startPosition = new Vector3(direction.x, direction.y, transform.position.z) * 0.7f;
			thisTransform.position += startPosition;

			movingProjectile.SetUpBulletTransform(direction, fromWeapon.ProjectileVelocity);

			foreach (var behaviour in behavioursOnShoot)
			{
				behaviour.OnShoot(direction, controlledByPlayer,fromWeapon,targetZone);
			}
		}

		/// <summary>
		/// Despawns bullet if OnMove behaviours request it
		/// </summary>
		/// <param name="distanceTraveled"></param>
		/// <param name="worldPosition"> Actual world position of the moving projectile </param>
		/// <returns> Is despawning bullet? </returns>
		public bool ProcessMove(Vector3 distanceTraveled, Vector3 worldPosition)
		{
			foreach (var behaviour in behavioursOnMove)
			{
				if (behaviour.OnMove(distanceTraveled))
				{
					DespawnThis(new RaycastHit2D(), worldPosition);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Main method for processing ray cast hit
		/// </summary>
		/// <param name="hit"></param>
		public void ProcessRaycastHit(RaycastHit2D hit)
		{
			if (IsHitValid(hit) == false) return;

			if(hitProcessor.ProcessHit(hit, behavioursOnBulletHit) == false) return;

			DespawnThis(hit, hit.point);
		}

		/// <summary>
		/// Check if we hit anything
		/// and that hit is not the shooter
		/// </summary>
		/// <param name="hit"></param>
		/// <returns></returns>
		private bool IsHitValid(RaycastHit2D hit)
		{
			if (hit.collider == null) return false;
			if (isSuicide) return true;
			if (hit.collider.gameObject == shooter) return false;
			return true;
		}

		/// <summary>
		/// Despawn bullet and call all
		/// on despawn behaviours
		/// </summary>
		private void DespawnThis(RaycastHit2D hit, Vector2 point)
		{
			foreach (var behaviour in behavioursOnBulletDespawn)
			{
				behaviour.OnDespawn(hit, point);
			}
			Despawn.ClientSingle(gameObject);
		}
	}
}