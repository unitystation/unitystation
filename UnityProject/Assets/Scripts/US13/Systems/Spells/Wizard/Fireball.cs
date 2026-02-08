using UnityEngine;
using US13.HealthV2;
using US13.Managers;
using US13.Projectiles;

namespace US13.Systems.Spells.Wizard
{
	/// <summary>
	/// A type of spell that casts an explosive and incendiary ball of fire towards the target. ONI'SOMA! Blast them!
	/// </summary>
	public class Fireball : Spell
	{
		[SerializeField]
		private GameObject projectilePrefab = default;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition)
		{
			Vector3Int casterWorldPos = caster.Script.WorldPos;
			Vector2 castVector = clickPosition - casterWorldPos;

			ProjectileManager.InstantiateAndShoot(projectilePrefab, castVector, caster.GameObject,
				null, BodyPartType.None);
			return true;
		}
	}
}
