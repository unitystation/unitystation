using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Creates a raycast at a certain point and process IOnHit behaviours
	/// Script is used by Proto-Kinetic-Accelerator for damaging everything on one tile
	/// </summary>
	public class ProjectileDamageAtPoint : MonoBehaviour, IOnShoot, IOnDespawn
	{
		private IOnHit[] behavioursOnBulletHit;
		private Vector2 direction;

		[SerializeField] private LayerMask layersToHit = default;

		private void Awake()
		{
			behavioursOnBulletHit = GetComponents<IOnHit>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
		}

		public void OnDespawn(RaycastHit2D hit, Vector2 point)
		{
			if (hit.collider == null)
			{
				RayCastAt(point);
			}
			else
			{
				var bulletHitTarget = Vector3.zero;
				bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
				bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;
				RayCastAt(bulletHitTarget);
			}
		}

		private void RayCastAt(Vector2 point)
		{
			var rayHits = Physics2D.RaycastAll(point, direction,0f, layersToHit);

			foreach (var rayHit in rayHits)
			{
				foreach (var behaviour in behavioursOnBulletHit)
				{
					behaviour.OnHit(rayHit);
				}
			}
		}
	}
}