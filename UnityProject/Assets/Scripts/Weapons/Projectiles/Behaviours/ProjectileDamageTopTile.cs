using System.Linq;
using Container.Gun;
using Container.HitConditions;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Script for damaging the top tile projectile collides with
	/// if tile is destroyed and not all damage was absorbed
	/// next layer will be hit with damage left
	/// </summary>
	public class ProjectileDamageTopTile : MonoBehaviour, IOnHitInteractTile
	{
		[SerializeField] private DamageData damageData = null;

		[SerializeField] private HitInteractTileCondition[] hitInteractTileConditions;

		public bool Interact(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var layers = interactableTiles.MetaTileMap.DamageableLayers;
			foreach (var layer in layers)
			{
				if (CheckConditions(hit, interactableTiles, worldPosition));

				if (layer.TilemapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, worldPosition) <= 0) continue;

				return true;
			}

			return false;
		}

		private bool CheckConditions(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			return hitInteractTileConditions.Any(condition => condition.CheckCondition(hit, interactableTiles, worldPosition));
		}
	}
}