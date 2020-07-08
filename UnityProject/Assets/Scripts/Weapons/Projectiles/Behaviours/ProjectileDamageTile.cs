using System.Linq;
using Container.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Script to damage specific tile projectile collided with
	/// </summary>
	public class ProjectileDamageTile : MonoBehaviour, IOnHit
	{
		[SerializeField] private DamageData damageData = null;

		[Tooltip("Tile layers to damage(Walls, Window, etc.)")]
		[SerializeField] private LayerType[] layersToHit = null;

		public bool OnHit(RaycastHit2D hit)
		{
			return TryHit(hit);
		}

		private bool TryHit(RaycastHit2D hit)
		{
			var tileMapDamage = hit.collider.GetComponent<TilemapDamage>();
			if (tileMapDamage == null) return false;

			if (IsHit(tileMapDamage.Layer.LayerType) == false) return false;

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			tileMapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, bulletHitTarget);

			return true;
		}

		private bool IsHit(LayerType layerType) => layersToHit.Any(l => l == layerType);
	}
}