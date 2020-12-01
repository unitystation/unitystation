using System;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	public class ProjectileKineticDamageCalculation: MonoBehaviour, IOnShoot
	{
		private float pressure;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			pressure = GetPressureOnPoint(shooter);
		}

		private static float GetPressureOnPoint(GameObject shooter)
		{
			var localPosition = shooter.transform.localPosition;

			float pressure = MatrixManager.AtPoint(
				localPosition.RoundToInt(), true
			).MetaDataLayer.Get(localPosition.RoundToInt()).GasMix.Pressure;

			return pressure;
		}

		public float DamageByPressureModifier(float maxDamage)
		{
			float newDamage = maxDamage - (maxDamage * (pressure / 135));
			newDamage = (float)Math.Round(newDamage);

			return Mathf.Clamp(newDamage, 0, maxDamage);
		}
	}
}