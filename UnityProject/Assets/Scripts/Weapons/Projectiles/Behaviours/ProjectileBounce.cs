using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileBounce : MonoBehaviour, IOnShoot, IOnHit
	{
		private Bullet bullet;

		private Vector2 direction;
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;

		[SerializeField] private int maxHitCount = 4;
		private int currentCount = 0;

		private void Awake()
		{
			bullet = GetComponent<Bullet>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			var interactableTile = hit.collider.GetComponentInParent<InteractableTiles>();

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			var tile = interactableTile.MetaTileMap.GetTileAtWorldPos(bulletHitTarget,LayerType.Walls);
			if (tile == null) return true;

			var normal = hit.normal;
			var newDirection = direction - 2*(direction * normal) * normal;

			bullet.Shoot(newDirection * 2f, shooter, weapon, targetZone);

			currentCount++;
			if (currentCount >= maxHitCount)
			{
				currentCount = 0;
				return true;
			}

			return false;
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