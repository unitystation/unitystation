using _3D;
using UnityEngine;
using NaughtyAttributes;
using ScriptableObjects.Gun;
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

		[SerializeField] private HitProcessor hitProcessor = null;
		[SerializeField] private LayerMaskData maskData = null;

		[Tooltip("If unchecked, projectile speed will be derived from the source weapon, if available.")]
		[SerializeField]
		private bool overrideVelocity = false;
		[SerializeField, ShowIf(nameof(overrideVelocity)), Range(1, 100)]
		private int projectileVelocity = 10;

		public LayerMaskData MaskData => maskData;

		[SerializeField]
		private PlayerTypes playerThatCanBeHit = PlayerTypes.Normal | PlayerTypes.Alien;

		private GameObject shooter;

		private bool destroyed;
		public bool Destroyed => destroyed;

		public bool WillHurtShooter { get; set; }

		private void Awake()
		{
			behavioursOnShoot = GetComponents<IOnShoot>();
			behavioursOnMove = GetComponentsInParent<IOnMove>();
			behavioursOnBulletHit = GetComponents<IOnHit>();
			behavioursOnBulletDespawn = GetComponents<IOnDespawn>();

			movingProjectile = GetComponentInChildren<MovingProjectile>();

			thisTransform = transform;


			if (Manager3D.Is3D && GameData.IsHeadlessServer == false)
			{
				var Handler = this.GetComponentInChildren<SpriteHandler>(); //So it doesn't mess up the movement of the bullet
				var Is3D =Handler.gameObject.AddComponent<ConvertTo3D>();
				Is3D?.DoConvertTo3D();
			}
		}

		public override void Suicide(GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			WillHurtShooter = true;
			StartShoot(Vector2.zero, controlledByPlayer, fromWeapon, targetZone);
		}

		public override void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			WillHurtShooter = false;
			StartShoot(direction, controlledByPlayer, fromWeapon, targetZone);
		}

		private void StartShoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, BodyPartType targetZone)
		{
			shooter = controlledByPlayer;

			var startPosition = new Vector3(direction.x, direction.y, thisTransform.position.z) * 0.2f;
			thisTransform.position += startPosition;

			if (overrideVelocity == false && fromWeapon != null)
			{
				projectileVelocity = fromWeapon.ProjectileVelocity;
			}

			movingProjectile.SetUpBulletTransform(direction, projectileVelocity);

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
		/// <param name="previousWorldPosition"> Previous world position of the moving projectile</param>
		/// <returns> Is despawning bullet? </returns>
		public bool ProcessMove(Vector3 distanceTraveled, Vector3 worldPosition, Vector3 previousWorldPosition)
		{
			foreach (var behaviour in behavioursOnMove)
			{
				if (behaviour.OnMove(distanceTraveled, previousWorldPosition))
				{
					DespawnThis(new MatrixManager.CustomPhysicsHit(), worldPosition);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Main method for processing ray cast hit
		/// </summary>
		/// <param name="hit"></param>
		public void ProcessRaycastHit(MatrixManager.CustomPhysicsHit hit)
		{
			if (IsHitValid(hit) == false) return;

			if(hitProcessor.ProcessHit(hit, behavioursOnBulletHit) == false) return;

			DespawnThis(hit, hit.HitWorld);
		}

		/// <summary>
		/// Check if we hit anything
		/// and that hit is not the shooter
		/// </summary>
		/// <param name="hit"></param>
		/// <returns></returns>
		private bool IsHitValid(MatrixManager.CustomPhysicsHit  hit)
		{
			if (hit.ItHit == false) return false;
			if (hit.CollisionHit.GameObject == shooter && WillHurtShooter == false) return false;

			if (hit.CollisionHit.GameObject != null &&
			    hit.CollisionHit.GameObject.TryGetComponent<PlayerScript>(out var playerScript) &&
			    playerThatCanBeHit.HasFlag(playerScript.PlayerType) == false)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Despawn bullet and call all
		/// on despawn behaviours
		/// </summary>
		private void DespawnThis(MatrixManager.CustomPhysicsHit  hit, Vector2 point)
		{
			destroyed = true;

			foreach (var behaviour in behavioursOnBulletDespawn)
			{
				behaviour.OnDespawn(hit, point);
			}

			_ = Despawn.ServerSingle(gameObject);
		}

		private void OnDisable()
		{
			shooter = null;
			WillHurtShooter = false;
			destroyed = false;
		}
	}
}
