using UnityEngine;
using ScriptableObjects.Gun;

namespace Weapons.Projectiles.Behaviours
{
	/// <summary>
	/// Damages integrity on collision
	/// only for kenetic weapons
	/// </summary>
	public class KeneticProjectileDamageInterity : MonoBehaviour, IOnShoot, IOnHit
	{
		private GameObject shooter;
		private Gun weapon;

		[SerializeField] private DamageData damageData = null;

		public void OnShoot(Vector2 direction, GameObject shooter, Gun weapon, BodyPartType targetZone = BodyPartType.Chest)
		{
			this.shooter = shooter;
			this.weapon = weapon;
		}

		public bool OnHit(RaycastHit2D hit)
		{
			return TryDamage(hit);
		}

		private bool TryDamage(RaycastHit2D hit)
		{
			float pressure = MatrixManager.AtPoint((Vector3Int)hit.point.To2Int(), true).MetaDataLayer.Get(hit.transform.localPosition.RoundToInt()).GasMix.Pressure;
			var newDamage = damageData.Damage;
			var coll = hit.collider;
			var integrity = coll.GetComponent<Integrity>();
			if (integrity == null) return false;
			// checks if its a high atmosphere 

			newDamage = 40 * (Mathf.Clamp((-pressure / 135), -1.0f, 0.0f) + 1);

			integrity.ApplyDamage(newDamage, damageData.AttackType, damageData.DamageType);

			Chat.AddAttackMsgToChat(shooter, coll.gameObject, BodyPartType.None, weapon.gameObject);
			Logger.LogTraceFormat("Hit {0} for {1} with HealthBehaviour! bullet absorbed", Category.Firearms,
				integrity.gameObject.name, damageData.Damage);

			return true;
		}

		private void OnDisable()
		{
			weapon = null;
			shooter = null;
		}
	}
}

