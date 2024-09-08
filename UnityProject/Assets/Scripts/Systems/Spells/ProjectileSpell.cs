using UnityEngine;
using Weapons.Projectiles;

namespace Systems.Spells
{
	/// <summary>
	/// A type of spell that will shoot a projectile(s) when cast
	/// </summary>
	public class ProjectileSpell : Spell
	{
		[SerializeField]
		private GameObject projectilePrefab = default;

		/// <summary>
		/// How many projectiles do we fire each shot
		/// </summary>
		public int ProjectilesPerUse = 1;

		/// <summary>
		/// The random projectile spread of our shots
		/// </summary>
		public int RandomSpread = 0;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition)
		{
			Vector3Int casterWorldPos = caster.Script.WorldPos;
			Vector2 castVector = clickPosition - casterWorldPos;
			for(int i = 0; i < ProjectilesPerUse; i++)
				ProjectileManager.InstantiateAndShoot(projectilePrefab, castVector, caster.GameObject,
					null, BodyPartType.None);
			return true;
		}
	}
}