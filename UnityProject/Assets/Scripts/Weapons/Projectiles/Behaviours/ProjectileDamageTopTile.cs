using System.Linq;
using Container.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Script for damaging the top tile projectile collides with
	/// if tile is destroyed and not all damage was absorbed
	/// next layer will be hit with damage left
	/// </summary>
	public class ProjectileDamageTopTile : MonoBehaviour, IOnHit
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
			var layerMetaTile = hit.collider.GetComponentInParent<MetaTileMap>();
			var layers = layerMetaTile.DamageableLayers;

			var bulletHitTarget = Vector3.zero;
			bulletHitTarget.x = hit.point.x - 0.01f * hit.normal.x;
			bulletHitTarget.y = hit.point.y - 0.01f * hit.normal.y;

			foreach (var layer in layers)
			{
				if(IsHit(layer.LayerType) == false) continue;

				if (layer.TilemapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, bulletHitTarget) <= 0) continue;

				return true;
			}

			return false;
		}

		private bool IsHit(LayerType layerType) => layersToHit.Any(l => l == layerType);
	}
}