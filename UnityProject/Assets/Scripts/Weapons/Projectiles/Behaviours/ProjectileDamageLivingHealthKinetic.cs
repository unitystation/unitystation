using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// only for kinetic weapons
	/// </summary>
	public class ProjectileDamageLivingHealthKinetic : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;
		private BodyPartType targetZone;


		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
			this.targetZone = targetZone;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			var coll = hit.collider;
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth == null)
			{
				return false;
			}

			float pressure = MatrixManager.AtPoint(
				(Vector3Int)hit.point.To2Int(),
				true
			).MetaDataLayer.Get(hit.transform.localPosition.RoundToInt()).GasMix.Pressure;

			var newDamage = DamageByPressureModifier(pressure);

			livingHealth.ApplyDamageToBodypart(shooter, newDamage, damageData.AttackType, damageData.DamageType, targetZone);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, targetZone, weapon.gameObject);

			Logger.LogTraceFormat(
				"Hit {0} for {1} with HealthBehaviour! bullet absorbed",
				Category.Firearms,
				livingHealth.gameObject.name,
				newDamage);

			return true;
		}

		private float DamageByPressureModifier(float pressure)
		{
			float newDamage = damageData.Damage * (-pressure / 135);
			return Mathf.Clamp(newDamage, -1.0f, 0.0f) + 1;
		}

		private void OnDisable()
		{
			shooter = null;
			weapon = null;
		}
	}
}

