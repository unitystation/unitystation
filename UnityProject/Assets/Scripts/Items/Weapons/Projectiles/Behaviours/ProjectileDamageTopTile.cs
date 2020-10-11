using System.Linq;
using ScriptableObjects.Gun;
using ScriptableObjects.Gun.HitConditions.Tile;
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

		[Tooltip("Tile layers to damage(Walls, Window, etc.)")]
		[SerializeField] private CheckLayerType layerType = null;

		public bool Interact(RaycastHit2D hit, InteractableTiles interactableTiles, Vector3 worldPosition)
		{
			var layers = interactableTiles.MetaTileMap.DamageableLayers;
			foreach (var layer in layers)
			{
				if(layerType.CheckType(layer.LayerType) == false) continue;

				if (layer.TilemapDamage.ApplyDamage(damageData.Damage, damageData.AttackType, worldPosition) <= 0) continue;

				return true;
			}

			return false;
		}
	}
}
