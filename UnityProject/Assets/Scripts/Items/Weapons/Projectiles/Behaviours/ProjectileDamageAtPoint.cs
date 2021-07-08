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

		[SerializeField] private LayerTypeSelection LayerTypeSelection =
			LayerTypeSelection.Grills | LayerTypeSelection.Walls | LayerTypeSelection.Windows;

		private void Awake()
		{
			behavioursOnBulletHit = GetComponents<IOnHit>();
		}

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon,
			BodyPartType targetZone = BodyPartType.Chest)
		{
			this.direction = direction;
		}

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			if (hit.CollisionHit.GameObject == null)
			{
				RayCastAt(point);
			}
			else
			{
				var bulletHitTarget = Vector3.zero;
				bulletHitTarget.x = hit.HitWorld.x - 0.01f * hit.Normal.x;
				bulletHitTarget.y = hit.HitWorld.y - 0.01f * hit.Normal.y;
				RayCastAt(bulletHitTarget);
			}
		}

		private void RayCastAt(Vector2 point)
		{
			var rayHits = MatrixManager.RayCast(point, direction, 0f, LayerTypeSelection, layersToHit);
			//var rayHits = Physics2D.RaycastAll(point, direction,0f, layersToHit);


			foreach (var behaviour in behavioursOnBulletHit)
			{
				behaviour.OnHit(rayHits);
			}
		}
	}
}