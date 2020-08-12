using PathFinding;
using ScriptableObjects.Gun;
using UnityEngine;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages health on collision
	/// </summary>
	public class ProjectileDamageLivingHealth : MonoBehaviour, IOnShoot, IOnHit
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
			var newDamage = damageData.Damage;
			var coll = hit.collider;
			var livingHealth = coll.GetComponent<LivingHealthBehaviour>();
			if (livingHealth == null) return false;
			// checks if its a kinetic weapon is a high atmosphere
			if (damageData.DamageType== DamageType.Kinetic)
			{
				
				if(MatrixManager.AtPoint((Vector3Int)hit.point.To2Int(), true).MetaDataLayer.Get(hit.transform.localPosition.RoundToInt()).GasMix.Pressure <= 50)
				{
					newDamage=damageData.Damage * .25f;
				}
			}

			livingHealth.ApplyDamageToBodypart(shooter, newDamage, damageData.AttackType, damageData.DamageType, targetZone);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, targetZone, weapon.gameObject);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
				livingHealth.gameObject.name, damageData.Damage);

			return true;
		}

		private void OnDisable()
		{
			shooter = null;
			weapon = null;
		}
	}
}