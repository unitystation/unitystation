using System;
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
		public float RandomSpread = 0;

		/// <summary>
		/// If we fire multiple projectiles do we change spread based on iteration
		/// </summary>
		public bool useIterativeSpread = true;

		public override bool CastSpellServer(PlayerInfo caster, Vector3 clickPosition)
		{
			Vector3Int casterWorldPos = caster.Script.WorldPos;
			Vector2 castVector = clickPosition - casterWorldPos;
			for(int i = 0; i < ProjectilesPerUse; i++)
				{
				ProjectileManager.InstantiateAndShoot(projectilePrefab, CalcProjectileDirections(castVector, i), caster.GameObject, null, BodyPartType.None);
				}
			return true;
		}

		private Vector2 CalcProjectileDirections(Vector2 direction, int iteration)
		{
			if (iteration == 0 ? RandomSpread == 0f : (!useIterativeSpread && RandomSpread == 0f)) return direction;

			//This is for shotgun spread and similar multi-projectile weapons
			float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
			float angleDeviation = 0;
			if(useIterativeSpread && iteration != 0)
			{
				float angleVariance = iteration / 1f;
				angleDeviation = Convert.ToBoolean(iteration & 1) ? angleVariance : -angleVariance;
			}

			if(RandomSpread != 0f)
			{
				angleDeviation += UnityEngine.Random.Range(RandomSpread, -RandomSpread);
			}
			float newAngle = (angle + angleDeviation) * Mathf.Deg2Rad;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		private Vector2 ApplyRecoil(Vector2 target)
		{
			float angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
			float angleVariance = SpreadVarianceRandomFloat(-RandomSpread, RandomSpread);
			float newAngle = angle * Mathf.Deg2Rad + angleVariance;
			Vector2 vec2 = new Vector2(Mathf.Cos(newAngle), Mathf.Sin(newAngle)).normalized;
			return vec2;
		}

		private float SpreadVarianceRandomFloat(float min, float max)
		{
			return UnityEngine.Random.value * (max - min) + min;
		}
	}
}