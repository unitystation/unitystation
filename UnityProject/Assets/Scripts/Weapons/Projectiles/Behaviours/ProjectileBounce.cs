using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileBounce : MonoBehaviour, IOnShoot, IOnHitInteractTile
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

		public bool Interact(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var tile = interactableTiles.MetaTileMap.GetTileAtWorldPos(worldPosition,LayerType.Walls);
			if (tile == null) return true;

			var normal = hit.normal;
			var newDirection = direction - 2*(direction * normal) * normal;

			bullet.Shoot(newDirection * 2f, shooter, weapon, targetZone);

			return IsCountReached();
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