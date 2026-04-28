using System.Linq;
using UnityEngine;
using US13.HealthV2;
using US13.Items.Weapons;
using US13.Managers.MatrixManager;
using US13.ScriptableObjects.Gun.HitConditions.Tile;
using US13.Tilemaps.Behaviours;

namespace US13.Projectiles.Behaviours
{
	public class ProjectileBounce : MonoBehaviour, IOnShoot, IOnHitInteractTile
	{
		private Bullet bullet;
		private Transform movingProjectile;

		private Vector2 direction;
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;
		private GameObject target;

		private MagazineBehaviour MagazineBehaviour;

		[SerializeField] private ConditionsTileArray hitInteractTileConditions = null;

		[SerializeField] private int maxHitCount = 4;
		private int currentCount = 0;


		private void Awake()
		{
			bullet = GetComponent<Bullet>();
			movingProjectile = GetComponentInChildren<MovingProjectile>().transform;
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, MagazineBehaviour MagazineBehaviour, BodyPartType targetZone = BodyPartType.Chest, GameObject Target = null)
		{
			this.direction = direction;
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
			this.MagazineBehaviour = MagazineBehaviour;
			this.target = Target;
		}

		public bool Interact(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			if (CheckConditions(hit, interactableTiles, worldPosition) == false) return true;

			movingProjectile.position = hit.HitWorld;
			RotateBullet(GetNewDirection(hit));

			return IsCountReached();
		}

		private bool CheckConditions(MatrixManager.CustomPhysicsHit hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			return hitInteractTileConditions.Conditions.Any(condition => condition.CheckCondition(hit, interactableTiles, worldPosition));
		}

		private void RotateBullet(Vector2 newDirection)
		{
			bullet.Shoot(newDirection, shooter, weapon, MagazineBehaviour, target, targetZone);
			bullet.WillHurtShooter = true;
		}

		private Vector2 GetNewDirection(MatrixManager.CustomPhysicsHit hit)
		{
			var normal = hit.Normal;
			var newDirection = direction - 2 * (direction * normal) * normal;
			return newDirection;
		}

		private bool IsCountReached()
		{
			currentCount++;
			if (currentCount < maxHitCount) return false;
			currentCount = 0;
			return true;
		}

		private void OnDisable()
		{
			direction = Vector2.zero;
			shooter = null;
			weapon = null;
			targetZone = BodyPartType.None;
			currentCount = 0;
		}
	}
}
