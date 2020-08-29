using System.Linq;
using ScriptableObjects.Gun.HitConditions.Tile;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileBounce : MonoBehaviour, IOnShoot, IOnHitInteractTile
	{
		private Bullet bullet;
		private Transform movingProjectile;

		private Vector2 direction;
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;

		[SerializeField] private ConditionsTileArray hitInteractTileConditions = null;

		[SerializeField] private int maxHitCount = 4;
		private int currentCount = 0;


		private void Awake()
		{
			bullet = GetComponent<Bullet>();
			movingProjectile = GetComponentInChildren<MovingProjectile>().transform;
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
		}

		public bool Interact(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			if (CheckConditions(hit, interactableTiles, worldPosition) == false) return true;

			movingProjectile.position = hit.point;
			RotateBullet(GetNewDirection(hit));

			return IsCountReached();
		}

		private bool CheckConditions(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			return hitInteractTileConditions.Conditions.Any(condition => condition.CheckCondition(hit, interactableTiles, worldPosition));
		}

		private void RotateBullet(Vector2 newDirection)
		{
			bullet.Shoot(newDirection, shooter, weapon, targetZone);
			bullet.WillHurtShooter = true;
		}

		private Vector2 GetNewDirection(RaycastHit2D hit)
		{
			var normal = hit.normal;
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
