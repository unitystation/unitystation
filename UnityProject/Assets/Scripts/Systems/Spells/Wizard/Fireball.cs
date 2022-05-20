using System.Collections;
using Messages.Server;
using UnityEngine;
using Weapons;
using Weapons.Projectiles;

namespace Systems.Spells.Wizard
{
	/// <summary>
	/// A type of spell that casts an explosive and incendiary ball of fire towards the target. ONI'SOMA! Blast them!
	/// </summary>
	public class Fireball : Spell
	{
		[SerializeField]
		private GameObject projectilePrefab = default;

		public override bool CastSpellServer(ConnectedPlayer caster, Vector3 clickPosition)
		{
			Vector3Int casterWorldPos = caster.Script.WorldPos;
			Vector2 castVector = clickPosition - casterWorldPos;

			ProjectileManager.InstantiateAndShoot(projectilePrefab, castVector, caster.GameObject,
				null, BodyPartType.None);
			return true;
		}
	}
}
