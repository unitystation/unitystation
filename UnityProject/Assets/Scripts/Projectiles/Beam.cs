using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;

namespace Weapons.Projectiles
{
	public class Beam : Projectile
	{
		private IOnShoot[] behavioursOnShoot;
		private IOnDespawn[] behavioursOnBulletDespawn;

		[Tooltip("Beam length in tiles.")]
		[SerializeField] private float distance = 10;

		[Tooltip("Layers to hit with raycast.")]
		[SerializeField] private LayerMaskData maskData = null;

		private void Awake()
		{
			behavioursOnShoot = GetComponents<IOnShoot>();
			behavioursOnBulletDespawn = GetComponents<IOnDespawn>();
		}

		public override void Suicide(GameObject controlledByPlayer, Gun fromWeapon, MagazineBehaviour Magazine, BodyPartType targetZone = BodyPartType.Chest)
		{
			ShootBeam(Vector2.zero, controlledByPlayer,fromWeapon,Magazine , targetZone);
		}

		public override void Shoot(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon,  MagazineBehaviour Magazine, BodyPartType targetZone = BodyPartType.Chest)
		{
			ShootBeam(direction, controlledByPlayer,fromWeapon, Magazine, targetZone);
		}

		private void ShootBeam(Vector2 direction, GameObject controlledByPlayer, Gun fromWeapon, MagazineBehaviour Magazine, BodyPartType targetZone )
		{
			foreach (var behaviour in behavioursOnShoot)
			{
				behaviour.OnShoot(direction, controlledByPlayer, fromWeapon, Magazine, targetZone);
			}

			var pos = transform.position;
			Vector3 startPos = new Vector3(direction.x, direction.y, pos.z) * 0.7f;
			var hit = MatrixManager.RayCast(pos + startPos, direction, distance - 1,maskData.TileMapLayers , maskData.Layers);

			var dis = ((Vector2) pos + (direction * distance));
			foreach (var behaviour in behavioursOnBulletDespawn)
			{
				behaviour.OnDespawn(hit,dis);
			}

			_ = Despawn.ServerSingle(gameObject);
		}
	}
}
